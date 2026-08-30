import { useEffect, useRef, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowDown,
  ArrowUp,
  ExternalLink,
  FileText,
  Link2,
  Loader2,
  Lock,
  Paperclip,
  Plus,
  Trash2,
  Upload,
} from "lucide-react";
import { toast } from "sonner";
import {
  addLessonMaterial,
  getLessonMaterials,
  MATERIAL_KINDS,
  removeLessonMaterial,
  reorderLessonMaterials,
  type AddLessonMaterialInput,
  type MaterialKind,
} from "@/api/curriculum";
import { Visibility } from "@/api/files";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Combobox, Field } from "@/components/list";
import { useFileUpload } from "@/hooks/use-file-upload";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

const KIND_LABEL: Record<MaterialKind, string> = {
  File: "Файл",
  Video: "Видео",
  Link: "Ссылка",
  Homework: "Домашнее задание",
  Presentation: "Презентация",
};

const DOC_EXTS = [".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".csv"];
const MAX_BYTES = 25 * 1024 * 1024;

export function LessonMaterialsPanel({
  lessonId,
  canManage,
}: {
  lessonId: string;
  canManage: boolean;
}) {
  const materialsKey = ["lesson", lessonId, "materials"] as const;
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: materialsKey,
    queryFn: () => getLessonMaterials(lessonId),
  });
  const materials = [...(query.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: materialsKey });

  const removeMutation = useMutation({
    mutationFn: (materialId: string) => removeLessonMaterial(materialId),
    onSuccess: () => {
      toast.success("Материал удалён");
      void invalidate();
    },
    onError: (err) => toast.error("Не удалось удалить материал", { description: describe(err) }),
  });

  const reorderMutation = useMutation({
    mutationFn: (orderedMaterialIds: string[]) =>
      reorderLessonMaterials({ lessonId, orderedMaterialIds }),
    onSuccess: () => void invalidate(),
    onError: (err) => toast.error("Не удалось изменить порядок", { description: describe(err) }),
  });

  const move = (index: number, dir: -1 | 1) => {
    const ids = materials.map((m) => m.id);
    const target = index + dir;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    reorderMutation.mutate(ids);
  };

  return (
    <div className="space-y-3">
      {query.isLoading ? (
        <p className="text-[12px] text-[var(--color-muted-foreground)]">Загрузка материалов…</p>
      ) : query.isError ? (
        <p className="text-[12px] text-[var(--color-destructive)]">{describe(query.error)}</p>
      ) : materials.length === 0 ? (
        <p className="text-[12px] italic text-[var(--color-muted-foreground)]">
          Материалов пока нет.
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.4)] rounded-lg border border-[var(--color-border)]">
          {materials.map((m, i) => (
            <li key={m.id} className="flex items-center gap-2 px-3 py-2">
              <span className="grid size-7 shrink-0 place-items-center rounded-md bg-[var(--color-muted)] text-[var(--color-muted-foreground)]">
                {m.url ? <Link2 className="size-3.5" /> : <Paperclip className="size-3.5" />}
              </span>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5">
                  <span className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
                    {m.title}
                  </span>
                  {!m.visibleToStudents && (
                    <span
                      title="Только для преподавателя"
                      className="inline-flex items-center gap-0.5 rounded bg-[var(--color-muted)] px-1 py-0.5 text-[9px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]"
                    >
                      <Lock className="size-2.5" />
                      скрыт
                    </span>
                  )}
                </div>
                <span className="text-[11px] text-[var(--color-muted-foreground)]">
                  {KIND_LABEL[m.kind]}
                  {m.url ? ` · ${m.url}` : ""}
                </span>
              </div>

              {m.url && (
                <a
                  href={m.url}
                  target="_blank"
                  rel="noreferrer"
                  className="grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
                  aria-label="Открыть ссылку"
                >
                  <ExternalLink className="size-3.5" />
                </a>
              )}

              {canManage && (
                <div className="flex shrink-0 items-center gap-0.5">
                  <button
                    type="button"
                    aria-label="Выше"
                    onClick={() => move(i, -1)}
                    disabled={i === 0 || reorderMutation.isPending}
                    className="grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] disabled:opacity-30"
                  >
                    <ArrowUp className="size-3.5" />
                  </button>
                  <button
                    type="button"
                    aria-label="Ниже"
                    onClick={() => move(i, 1)}
                    disabled={i === materials.length - 1 || reorderMutation.isPending}
                    className="grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] disabled:opacity-30"
                  >
                    <ArrowDown className="size-3.5" />
                  </button>
                  <button
                    type="button"
                    aria-label={`Удалить материал ${m.title}`}
                    onClick={() => removeMutation.mutate(m.id)}
                    disabled={removeMutation.isPending}
                    className="grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-destructive)] disabled:opacity-30"
                  >
                    <Trash2 className="size-3.5" />
                  </button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {canManage && <AddMaterialForm lessonId={lessonId} onAdded={invalidate} />}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Add-material form — file XOR link, validated client-side.
// ───────────────────────────────────────────────────────────────────────

function AddMaterialForm({
  lessonId,
  onAdded,
}: {
  lessonId: string;
  onAdded: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [source, setSource] = useState<"file" | "link">("link");
  const [title, setTitle] = useState("");
  const [kind, setKind] = useState<MaterialKind>("Link");
  const [url, setUrl] = useState("");
  const [visibleToStudents, setVisibleToStudents] = useState(true);
  const [pickedFileId, setPickedFileId] = useState<string | null>(null);
  const [pickedFileName, setPickedFileName] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { upload, isUploading, reset: resetUpload } = useFileUpload({
    ownerType: "LessonMaterial",
    ownerId: lessonId,
    category: "Document",
    visibility: Visibility.Private,
    allowedExtensions: DOC_EXTS,
    maxBytes: MAX_BYTES,
  });

  const resetForm = () => {
    setSource("link");
    setTitle("");
    setKind("Link");
    setUrl("");
    setVisibleToStudents(true);
    setPickedFileId(null);
    setPickedFileName(null);
    resetUpload();
  };

  useEffect(() => {
    if (!open) resetForm();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: AddLessonMaterialInput) => addLessonMaterial(input),
    onSuccess: () => {
      toast.success("Материал добавлен");
      onAdded();
      setOpen(false);
    },
    onError: (err) => toast.error("Не удалось добавить материал", { description: describe(err) }),
  });

  const handlePick = () => fileInputRef.current?.click();

  const onFileChange = async () => {
    const file = fileInputRef.current?.files?.[0];
    if (!file) return;
    try {
      const asset = await upload(file);
      setPickedFileId(asset.id);
      setPickedFileName(file.name);
      if (!title.trim()) setTitle(file.name);
    } catch (e) {
      toast.error("Загрузка не удалась", { description: describe(e) });
    }
  };

  // Exactly one of file / link — enforced by the mode toggle + this guard.
  const hasFile = source === "file" && !!pickedFileId;
  const hasLink = source === "link" && url.trim().length > 0;
  const exactlyOne = hasFile !== hasLink; // XOR
  const valid = title.trim().length > 0 && exactlyOne && !isUploading;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    mutation.mutate({
      lessonId,
      kind,
      title: title.trim(),
      fileId: source === "file" ? pickedFileId : null,
      url: source === "link" ? url.trim() : null,
      visibleToStudents,
    });
  };

  if (!open) {
    return (
      <Button
        type="button"
        size="sm"
        variant="outline"
        className="h-8 gap-1.5 px-3 text-[12px]"
        onClick={() => setOpen(true)}
      >
        <Plus className="size-3.5" />
        Добавить материал
      </Button>
    );
  }

  return (
    <form
      onSubmit={onSubmit}
      className="space-y-3 rounded-lg border border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.35)] p-3"
    >
      <div className="flex gap-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-0.5 text-[11px] font-semibold uppercase tracking-wider">
        <button
          type="button"
          onClick={() => setSource("link")}
          aria-pressed={source === "link"}
          className={cn(
            "flex h-7 flex-1 items-center justify-center gap-1.5 rounded-md px-3 transition-colors",
            source === "link"
              ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
              : "text-[var(--color-muted-foreground)]",
          )}
        >
          <Link2 className="size-3.5" />
          Ссылка
        </button>
        <button
          type="button"
          onClick={() => setSource("file")}
          aria-pressed={source === "file"}
          className={cn(
            "flex h-7 flex-1 items-center justify-center gap-1.5 rounded-md px-3 transition-colors",
            source === "file"
              ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
              : "text-[var(--color-muted-foreground)]",
          )}
        >
          <FileText className="size-3.5" />
          Файл
        </button>
      </div>

      <Field id="mat-title" label="Название материала" required>
        <Input
          id="mat-title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          className="h-8 text-[13px]"
        />
      </Field>

      <div className="grid gap-3 sm:grid-cols-2">
        <Field id="mat-kind" label="Тип">
          <Combobox
            id="mat-kind"
            label="Тип"
            value={kind}
            onChange={(v) => setKind((v as MaterialKind) ?? "Link")}
            options={MATERIAL_KINDS.map((k) => ({ value: k, label: KIND_LABEL[k] }))}
          />
        </Field>
        <div className="flex items-end justify-between rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] px-3 py-2">
          <div className="text-[11px] text-[var(--color-muted-foreground)]">
            Виден ученикам
          </div>
          <Switch
            checked={visibleToStudents}
            onCheckedChange={setVisibleToStudents}
            aria-label="Виден ученикам"
          />
        </div>
      </div>

      {source === "link" ? (
        <Field id="mat-url" label="URL" required>
          <Input
            id="mat-url"
            type="url"
            inputMode="url"
            placeholder="https://…"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            className="h-8 text-[13px]"
          />
        </Field>
      ) : (
        <div className="space-y-1.5">
          <span className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Файл
          </span>
          <div className="flex items-center gap-2">
            <input
              ref={fileInputRef}
              type="file"
              accept={DOC_EXTS.join(",")}
              className="hidden"
              onChange={() => void onFileChange()}
            />
            <Button
              type="button"
              size="sm"
              variant="outline"
              className="h-8 gap-1.5 px-3 text-[12px]"
              onClick={handlePick}
              disabled={isUploading}
            >
              {isUploading ? (
                <Loader2 className="size-3.5 animate-spin" />
              ) : (
                <Upload className="size-3.5" />
              )}
              {pickedFileName ? "Заменить файл" : "Выбрать файл"}
            </Button>
            <span className="truncate text-[12px] text-[var(--color-muted-foreground)]">
              {pickedFileName ?? "PDF, DOCX, XLSX, PPTX, TXT, CSV · до 25 МБ"}
            </span>
          </div>
        </div>
      )}

      <div className="flex items-center justify-end gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          className="h-8 px-3 text-[12px]"
          onClick={() => setOpen(false)}
          disabled={mutation.isPending}
        >
          Отмена
        </Button>
        <Button
          type="submit"
          size="sm"
          className="h-8 px-3 text-[12px]"
          disabled={!valid || mutation.isPending}
        >
          {mutation.isPending ? "Добавление…" : "Добавить"}
        </Button>
      </div>
    </form>
  );
}
