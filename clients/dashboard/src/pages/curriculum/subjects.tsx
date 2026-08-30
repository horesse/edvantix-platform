import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Check,
  ChevronDown,
  ChevronRight,
  FolderTree,
  ArrowDown,
  ArrowUp,
  Pencil,
  Plus,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  createSubject,
  deleteSubject,
  getSubjectTree,
  reorderSubjects,
  updateSubject,
  type SubjectNodeDto,
} from "@/api/curriculum";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  EntityEmpty,
  EntityPageHeader,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

const TREE_KEY = ["subjects", "tree"] as const;

export function SubjectsPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canCreate = perms.includes("Permissions.Curriculum.Subjects.Create");
  const canUpdate = perms.includes("Permissions.Curriculum.Subjects.Update");
  const canDelete = perms.includes("Permissions.Curriculum.Subjects.Delete");

  const [addingRoot, setAddingRoot] = useState(false);

  const query = useQuery({ queryKey: TREE_KEY, queryFn: getSubjectTree });
  const tree = query.data ?? [];

  const totalNodes = countNodes(tree);

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={FolderTree}
        title="Направления"
        total={query.data ? totalNodes : null}
        unit="направление"
        description="Дерево учебных направлений: разделы и подразделы, к которым привязываются курсы."
      >
        {canCreate && (
          <Button
            onClick={() => setAddingRoot(true)}
            className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
          >
            <Plus className="size-4" />
            Новое направление
          </Button>
        )}
      </EntityPageHeader>

      {query.isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div
              key={i}
              className="h-11 animate-pulse rounded-lg bg-[oklch(from_var(--color-muted)_l_c_h_/_0.6)]"
            />
          ))}
        </div>
      ) : query.isError ? (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      ) : tree.length === 0 && !addingRoot ? (
        <EntityEmpty
          icon={FolderTree}
          title="Дерево направлений пусто"
          body="Создайте первое направление — например «Английский язык» или «Математика»."
          action={
            canCreate ? (
              <Button onClick={() => setAddingRoot(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Новое направление
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs">
          <ul>
            {tree.map((node, i) => (
              <SubjectNode
                key={node.id}
                node={node}
                depth={0}
                index={i}
                siblings={tree}
                parentId={null}
                canCreate={canCreate}
                canUpdate={canUpdate}
                canDelete={canDelete}
              />
            ))}
          </ul>
          {addingRoot && (
            <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-3 py-2">
              <InlineCreate
                parentId={null}
                placeholder="Название направления"
                onDone={() => setAddingRoot(false)}
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function countNodes(nodes: SubjectNodeDto[]): number {
  return nodes.reduce((sum, n) => sum + 1 + countNodes(n.children), 0);
}

// ───────────────────────────────────────────────────────────────────────
//  Tree node
// ───────────────────────────────────────────────────────────────────────

function SubjectNode({
  node,
  depth,
  index,
  siblings,
  parentId,
  canCreate,
  canUpdate,
  canDelete,
}: {
  node: SubjectNodeDto;
  depth: number;
  index: number;
  siblings: SubjectNodeDto[];
  parentId: string | null;
  canCreate: boolean;
  canUpdate: boolean;
  canDelete: boolean;
}) {
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(true);
  const [renaming, setRenaming] = useState(false);
  const [addingChild, setAddingChild] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const hasChildren = node.children.length > 0;

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["subjects"] });

  const renameMutation = useMutation({
    mutationFn: (name: string) =>
      updateSubject({ subjectId: node.id, name, parentId }),
    onSuccess: () => {
      toast.success("Направление переименовано");
      void invalidate();
      setRenaming(false);
    },
    onError: (err) => toast.error("Не удалось переименовать", { description: describe(err) }),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteSubject(node.id),
    onSuccess: () => {
      toast.success("Направление удалено");
      void invalidate();
      setConfirmDelete(false);
    },
    onError: (err) => toast.error("Не удалось удалить", { description: describe(err) }),
  });

  const reorderMutation = useMutation({
    mutationFn: (orderedSubjectIds: string[]) =>
      reorderSubjects({ parentId, orderedSubjectIds }),
    onSuccess: () => void invalidate(),
    onError: (err) => toast.error("Не удалось изменить порядок", { description: describe(err) }),
  });

  const move = (dir: -1 | 1) => {
    const ids = siblings.map((s) => s.id);
    const target = index + dir;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    reorderMutation.mutate(ids);
  };

  return (
    <li>
      <div
        className="group flex items-center gap-1.5 border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)] px-3 py-2 transition-colors hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)]"
        style={{ paddingLeft: 12 + depth * 22 }}
      >
        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          className={cn(
            "grid size-5 shrink-0 place-items-center rounded text-[var(--color-muted-foreground)]",
            !hasChildren && "invisible",
          )}
          aria-label={expanded ? "Свернуть" : "Развернуть"}
        >
          {expanded ? <ChevronDown className="size-3.5" /> : <ChevronRight className="size-3.5" />}
        </button>

        {renaming ? (
          <InlineRename
            initial={node.name}
            pending={renameMutation.isPending}
            onSave={(name) => renameMutation.mutate(name)}
            onCancel={() => setRenaming(false)}
          />
        ) : (
          <>
            <span className="min-w-0 flex-1 truncate text-[13.5px] font-medium text-[var(--color-foreground)]">
              {node.name}
            </span>
            <span className="mr-1 hidden font-mono text-[10px] text-[var(--color-muted-foreground)] sm:inline">
              {node.slug}
            </span>

            <div className="flex shrink-0 items-center gap-0.5 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
              {canUpdate && (
                <>
                  <IconBtn
                    label="Выше"
                    onClick={() => move(-1)}
                    disabled={index === 0 || reorderMutation.isPending}
                  >
                    <ArrowUp className="size-3.5" />
                  </IconBtn>
                  <IconBtn
                    label="Ниже"
                    onClick={() => move(1)}
                    disabled={index === siblings.length - 1 || reorderMutation.isPending}
                  >
                    <ArrowDown className="size-3.5" />
                  </IconBtn>
                </>
              )}
              {canCreate && (
                <IconBtn label="Добавить подраздел" onClick={() => setAddingChild(true)}>
                  <Plus className="size-3.5" />
                </IconBtn>
              )}
              {canUpdate && (
                <IconBtn label="Переименовать" onClick={() => setRenaming(true)}>
                  <Pencil className="size-3.5" />
                </IconBtn>
              )}
              {canDelete && (
                <IconBtn
                  label="Удалить"
                  onClick={() => setConfirmDelete(true)}
                  danger
                >
                  <Trash2 className="size-3.5" />
                </IconBtn>
              )}
            </div>
          </>
        )}
      </div>

      {confirmDelete && (
        <div
          className="flex flex-wrap items-center gap-2 border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.05)] px-3 py-2 text-[12px] text-[var(--color-destructive)]"
          style={{ paddingLeft: 12 + depth * 22 }}
        >
          <span>
            Удалить «{node.name}»?{" "}
            {hasChildren ? "Внутри есть подразделы — сначала удалите их." : "Действие необратимо."}
          </span>
          <div className="ml-auto flex gap-2">
            <Button
              size="sm"
              variant="outline"
              className="h-7 px-2 text-[11px]"
              onClick={() => setConfirmDelete(false)}
              disabled={deleteMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              size="sm"
              variant="destructive"
              className="h-7 px-2 text-[11px]"
              onClick={() => deleteMutation.mutate()}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? "Удаление…" : "Удалить"}
            </Button>
          </div>
        </div>
      )}

      {addingChild && (
        <div
          className="border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)] px-3 py-2"
          style={{ paddingLeft: 12 + (depth + 1) * 22 }}
        >
          <InlineCreate
            parentId={node.id}
            placeholder="Название подраздела"
            onDone={() => setAddingChild(false)}
          />
        </div>
      )}

      {expanded && hasChildren && (
        <ul>
          {node.children.map((child, i) => (
            <SubjectNode
              key={child.id}
              node={child}
              depth={depth + 1}
              index={i}
              siblings={node.children}
              parentId={node.id}
              canCreate={canCreate}
              canUpdate={canUpdate}
              canDelete={canDelete}
            />
          ))}
        </ul>
      )}
    </li>
  );
}

function IconBtn({
  label,
  onClick,
  disabled,
  danger,
  children,
}: {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  danger?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-label={label}
      title={label}
      className={cn(
        "grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] transition-colors",
        "hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
        "disabled:cursor-not-allowed disabled:opacity-30",
        danger && "hover:text-[var(--color-destructive)]",
      )}
    >
      {children}
    </button>
  );
}

function InlineRename({
  initial,
  pending,
  onSave,
  onCancel,
}: {
  initial: string;
  pending: boolean;
  onSave: (name: string) => void;
  onCancel: () => void;
}) {
  const [value, setValue] = useState(initial);
  return (
    <form
      className="flex min-w-0 flex-1 items-center gap-1.5"
      onSubmit={(e) => {
        e.preventDefault();
        const name = value.trim();
        if (name) onSave(name);
      }}
    >
      <Input
        value={value}
        onChange={(e) => setValue(e.target.value)}
        autoFocus
        onKeyDown={(e) => e.key === "Escape" && onCancel()}
        aria-label="Название направления"
        className="h-8 flex-1 text-[13px]"
      />
      <IconBtn label="Сохранить" onClick={() => {
        const name = value.trim();
        if (name) onSave(name);
      }} disabled={pending}>
        <Check className="size-3.5" />
      </IconBtn>
      <IconBtn label="Отмена" onClick={onCancel} disabled={pending}>
        <X className="size-3.5" />
      </IconBtn>
    </form>
  );
}

function InlineCreate({
  parentId,
  placeholder,
  onDone,
}: {
  parentId: string | null;
  placeholder: string;
  onDone: () => void;
}) {
  const queryClient = useQueryClient();
  const [value, setValue] = useState("");

  useEffect(() => setValue(""), [parentId]);

  const mutation = useMutation({
    mutationFn: (name: string) => createSubject({ name, parentId }),
    onSuccess: () => {
      toast.success("Направление создано");
      void queryClient.invalidateQueries({ queryKey: ["subjects"] });
      onDone();
    },
    onError: (err) => toast.error("Не удалось создать", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const name = value.trim();
    if (name) mutation.mutate(name);
  };

  return (
    <form className="flex items-center gap-1.5" onSubmit={onSubmit}>
      <Input
        value={value}
        onChange={(e) => setValue(e.target.value)}
        autoFocus
        placeholder={placeholder}
        aria-label={placeholder}
        onKeyDown={(e) => e.key === "Escape" && onDone()}
        className="h-8 flex-1 text-[13px]"
      />
      <Button type="submit" size="sm" className="h-8 px-3 text-[12px]" disabled={mutation.isPending}>
        {mutation.isPending ? "…" : "Добавить"}
      </Button>
      <Button
        type="button"
        size="sm"
        variant="outline"
        className="h-8 px-3 text-[12px]"
        onClick={onDone}
        disabled={mutation.isPending}
      >
        Отмена
      </Button>
    </form>
  );
}
