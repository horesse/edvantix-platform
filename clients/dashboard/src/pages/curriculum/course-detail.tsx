import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
} from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  ArrowDown,
  ArrowUp,
  BookOpen,
  ChevronDown,
  ChevronRight,
  Clock,
  Copy,
  FolderTree,
  GraduationCap,
  Layers,
  Pencil,
  Plus,
  RefreshCw,
  Rocket,
  Archive as ArchiveIcon,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import {
  archiveCourse,
  createCourseModule,
  createLesson,
  deleteCourse,
  deleteCourseModule,
  deleteLesson,
  duplicateCourse,
  getCourseById,
  getSubjectTree,
  publishCourse,
  reorderCourseModules,
  reorderLessons,
  updateCourse,
  updateCourseModule,
  updateLesson,
  COURSE_LEVELS,
  type CourseDetailDto,
  type CourseLevel,
  type CourseModuleWithLessonsDto,
  type LessonDto,
  type UpdateCourseInput,
  type UpdateLessonInput,
} from "@/api/curriculum";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Combobox,
  EntityDetailBack,
  EntityDetailHero,
  EntityDetailAvatar,
  EntityDetailStat,
  EntityDetailMeta,
  EntityDetailSection,
  EntityStatusBadge,
  ErrorBand,
  Field,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { describe, formatDate } from "@/lib/list-helpers";
import { LessonMaterialsPanel } from "./lesson-materials-panel";
import { flattenSubjects, LEVEL_LABEL, STATUS_LABEL, STATUS_TONE } from "./curriculum-ui";

const TEXTAREA_CLS = cn(
  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
  "placeholder:text-[var(--color-muted-foreground)]",
  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
);

export function CourseBuilderPage() {
  const { courseId = "" } = useParams<{ courseId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const perms = useAuth().user?.permissions ?? [];
  const canUpdate = perms.includes("Permissions.Curriculum.Courses.Update");
  const canPublish = perms.includes("Permissions.Curriculum.Courses.Publish");
  const canDuplicate = perms.includes("Permissions.Curriculum.Courses.Create");
  const canDeleteCourse = perms.includes("Permissions.Curriculum.Courses.Delete");
  const canCreateLesson = perms.includes("Permissions.Curriculum.Lessons.Create");
  const canUpdateLesson = perms.includes("Permissions.Curriculum.Lessons.Update");
  const canDeleteLesson = perms.includes("Permissions.Curriculum.Lessons.Delete");
  const canViewMaterials = perms.includes("Permissions.Curriculum.LessonMaterials.View");
  const canManageMaterials = perms.includes("Permissions.Curriculum.LessonMaterials.Manage");

  const courseKey = ["course", courseId] as const;
  const query = useQuery({
    queryKey: courseKey,
    queryFn: () => getCourseById(courseId),
    enabled: !!courseId,
  });
  const course = query.data;

  const subjectsQuery = useQuery({
    queryKey: ["subjects", "tree"],
    queryFn: getSubjectTree,
    staleTime: 60_000,
  });
  const subjectOptions = useMemo(
    () => flattenSubjects(subjectsQuery.data ?? []),
    [subjectsQuery.data],
  );
  const subjectName = subjectOptions
    .find((o) => o.value === course?.subjectId)
    ?.label.trim();

  const invalidateCourse = useCallback(
    () => queryClient.invalidateQueries({ queryKey: courseKey }),
    [queryClient, courseId], // eslint-disable-line react-hooks/exhaustive-deps
  );

  const [editOpen, setEditOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [lifecycleError, setLifecycleError] = useState<string | null>(null);

  const publishMutation = useMutation({
    mutationFn: () => publishCourse(courseId),
    onSuccess: () => {
      toast.success("Курс опубликован");
      setLifecycleError(null);
      void invalidateCourse();
    },
    onError: (err) => {
      const msg =
        err instanceof ApiRequestError && err.status === 409
          ? err.problem?.detail ??
            "У курса нет ни одного раздела — добавьте раздел перед публикацией."
          : describe(err);
      setLifecycleError(msg);
      toast.error("Не удалось опубликовать курс", { description: msg });
    },
  });

  const archiveMutation = useMutation({
    mutationFn: () => archiveCourse(courseId),
    onSuccess: () => {
      toast.success("Курс архивирован");
      setLifecycleError(null);
      void invalidateCourse();
    },
    onError: (err) => toast.error("Не удалось архивировать", { description: describe(err) }),
  });

  const duplicateMutation = useMutation({
    mutationFn: () => duplicateCourse(courseId),
    onSuccess: (newId) => {
      toast.success("Создана копия курса");
      void queryClient.invalidateQueries({ queryKey: ["courses"] });
      navigate(`/courses/${newId}`);
    },
    onError: (err) => toast.error("Не удалось дублировать", { description: describe(err) }),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteCourse(courseId),
    onSuccess: () => {
      toast.success("Курс перемещён в корзину");
      void queryClient.invalidateQueries({ queryKey: ["courses"] });
      navigate("/courses");
    },
    onError: (err) => toast.error("Не удалось удалить", { description: describe(err) }),
  });

  return (
    <div className="pb-12">
      <EntityDetailBack to="/courses" label="К списку курсов" />

      {query.isError && (
        <div className="mb-5">
          <ErrorBand message={describe(query.error)} />
        </div>
      )}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка курса…</p>
      ) : course ? (
        <>
          <EntityDetailHero
            avatar={<EntityDetailAvatar name={course.title} icon={BookOpen} />}
            title={course.title}
            badges={
              <EntityStatusBadge tone={STATUS_TONE[course.status]}>
                {STATUS_LABEL[course.status]}
              </EntityStatusBadge>
            }
            subtitle={
              <span className="font-mono text-[11px]">{course.slug}</span>
            }
            actions={
              <>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => void query.refetch()}
                  disabled={query.isFetching}
                  className="gap-1.5"
                >
                  <RefreshCw className={cn("h-3.5 w-3.5", query.isFetching && "animate-spin")} />
                  <span className="hidden sm:inline">Обновить</span>
                </Button>
                {canUpdate && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setEditOpen(true)}
                    className="gap-1.5"
                  >
                    <Pencil className="h-3.5 w-3.5" />
                    <span className="hidden sm:inline">Изменить</span>
                  </Button>
                )}
                {canPublish && course.status !== "Published" && (
                  <Button
                    size="sm"
                    onClick={() => publishMutation.mutate()}
                    disabled={publishMutation.isPending}
                    className="gap-1.5"
                  >
                    <Rocket className="h-3.5 w-3.5" />
                    Опубликовать
                  </Button>
                )}
                {canPublish && course.status === "Published" && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => archiveMutation.mutate()}
                    disabled={archiveMutation.isPending}
                    className="gap-1.5"
                  >
                    <ArchiveIcon className="h-3.5 w-3.5" />
                    Архивировать
                  </Button>
                )}
                {canDuplicate && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => duplicateMutation.mutate()}
                    disabled={duplicateMutation.isPending}
                    className="gap-1.5"
                  >
                    <Copy className="h-3.5 w-3.5" />
                    <span className="hidden sm:inline">Дублировать</span>
                  </Button>
                )}
                {canDeleteCourse && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setConfirmDelete(true)}
                    className="gap-1.5 hover:!border-[var(--color-destructive)] hover:!text-[var(--color-destructive)]"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                    <span className="hidden sm:inline">Удалить</span>
                  </Button>
                )}
              </>
            }
            stats={
              <>
                <EntityDetailStat icon={GraduationCap} value={LEVEL_LABEL[course.level]} label="уровень" />
                <EntityDetailStat icon={Clock} value={`${course.durationHours} ч`} label="длительность" />
                <EntityDetailStat icon={Layers} value={course.modules.length} label="разделов" />
                <EntityDetailStat
                  icon={BookOpen}
                  value={course.modules.reduce((s, m) => s + m.lessons.length, 0)}
                  label="уроков"
                />
              </>
            }
            meta={
              <>
                {subjectName && (
                  <EntityDetailMeta icon={FolderTree}>
                    <Link to="/subjects" className="hover:text-[var(--color-foreground)]">
                      {subjectName}
                    </Link>
                  </EntityDetailMeta>
                )}
                <EntityDetailMeta icon={Clock} hideOnMobile>
                  Создан {formatDate(course.createdAtUtc)}
                </EntityDetailMeta>
                {course.publishedAtUtc && (
                  <EntityDetailMeta icon={Rocket} hideOnTablet>
                    Опубликован {formatDate(course.publishedAtUtc)}
                  </EntityDetailMeta>
                )}
              </>
            }
          />

          {lifecycleError && (
            <div className="mb-5">
              <ErrorBand message={lifecycleError} />
            </div>
          )}

          <EntityDetailSection
            title="Структура курса"
            icon={Layers}
            description="Разделы и уроки. Правки уроков сохраняются автоматически."
            padded={false}
          >
            <ModulesEditor
              course={course}
              canUpdate={canUpdate}
              canCreateLesson={canCreateLesson}
              canUpdateLesson={canUpdateLesson}
              canDeleteLesson={canDeleteLesson}
              canViewMaterials={canViewMaterials}
              canManageMaterials={canManageMaterials}
              onStructureChange={invalidateCourse}
            />
          </EntityDetailSection>

          <CourseEditDialog
            open={editOpen}
            course={course}
            subjectOptions={subjectOptions}
            onClose={() => setEditOpen(false)}
            onSaved={invalidateCourse}
          />

          <Dialog open={confirmDelete} onOpenChange={(o) => !o && setConfirmDelete(false)}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Удалить курс?</DialogTitle>
                <DialogDescription>
                  Курс «{course.title}» будет перемещён в корзину вместе со всеми
                  разделами и уроками. Его можно восстановить из корзины курсов.
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <DialogClose asChild>
                  <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
                    Отмена
                  </Button>
                </DialogClose>
                <Button
                  variant="destructive"
                  onClick={() => deleteMutation.mutate()}
                  disabled={deleteMutation.isPending}
                >
                  {deleteMutation.isPending ? "Удаление…" : "В корзину"}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </>
      ) : (
        <p className="text-sm text-[var(--color-muted-foreground)]">Курс не найден.</p>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Modules editor
// ───────────────────────────────────────────────────────────────────────

function ModulesEditor({
  course,
  canUpdate,
  canCreateLesson,
  canUpdateLesson,
  canDeleteLesson,
  canViewMaterials,
  canManageMaterials,
  onStructureChange,
}: {
  course: CourseDetailDto;
  canUpdate: boolean;
  canCreateLesson: boolean;
  canUpdateLesson: boolean;
  canDeleteLesson: boolean;
  canViewMaterials: boolean;
  canManageMaterials: boolean;
  onStructureChange: () => void;
}) {
  const [adding, setAdding] = useState(false);
  const [newTitle, setNewTitle] = useState("");

  const modules = [...course.modules].sort((a, b) => a.sortOrder - b.sortOrder);

  const createModuleMutation = useMutation({
    mutationFn: (title: string) => createCourseModule({ courseId: course.id, title }),
    onSuccess: () => {
      toast.success("Раздел добавлен");
      setNewTitle("");
      setAdding(false);
      onStructureChange();
    },
    onError: (err) => toast.error("Не удалось добавить раздел", { description: describe(err) }),
  });

  const reorderModulesMutation = useMutation({
    mutationFn: (orderedModuleIds: string[]) =>
      reorderCourseModules({ courseId: course.id, orderedModuleIds }),
    onSuccess: () => onStructureChange(),
    onError: (err) => {
      toast.error("Не удалось изменить порядок разделов", { description: describe(err) });
      onStructureChange();
    },
  });

  const moveModule = (index: number, dir: -1 | 1) => {
    const ids = modules.map((m) => m.id);
    const target = index + dir;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    reorderModulesMutation.mutate(ids);
  };

  return (
    <div>
      {modules.length === 0 ? (
        <div className="px-5 py-8 text-center text-[13px] text-[var(--color-muted-foreground)]">
          Разделов пока нет. Курс нельзя опубликовать без хотя бы одного раздела.
        </div>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {modules.map((m, i) => (
            <ModuleBlock
              key={m.id}
              module={m}
              index={i}
              total={modules.length}
              canUpdate={canUpdate}
              canCreateLesson={canCreateLesson}
              canUpdateLesson={canUpdateLesson}
              canDeleteLesson={canDeleteLesson}
              canViewMaterials={canViewMaterials}
              canManageMaterials={canManageMaterials}
              onMove={moveModule}
              onStructureChange={onStructureChange}
            />
          ))}
        </ul>
      )}

      {canUpdate && (
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-5 py-3">
          {adding ? (
            <form
              className="flex items-center gap-2"
              onSubmit={(e) => {
                e.preventDefault();
                const t = newTitle.trim();
                if (t) createModuleMutation.mutate(t);
              }}
            >
              <Input
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
                placeholder="Название раздела"
                aria-label="Название раздела"
                autoFocus
                className="h-9 flex-1 text-[13px]"
              />
              <Button type="submit" size="sm" disabled={createModuleMutation.isPending}>
                {createModuleMutation.isPending ? "…" : "Добавить"}
              </Button>
              <Button
                type="button"
                size="sm"
                variant="outline"
                onClick={() => {
                  setAdding(false);
                  setNewTitle("");
                }}
                disabled={createModuleMutation.isPending}
              >
                Отмена
              </Button>
            </form>
          ) : (
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={() => setAdding(true)}
            >
              <Plus className="size-4" />
              Добавить раздел
            </Button>
          )}
        </div>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Module block
// ───────────────────────────────────────────────────────────────────────

function ModuleBlock({
  module,
  index,
  total,
  canUpdate,
  canCreateLesson,
  canUpdateLesson,
  canDeleteLesson,
  canViewMaterials,
  canManageMaterials,
  onMove,
  onStructureChange,
}: {
  module: CourseModuleWithLessonsDto;
  index: number;
  total: number;
  canUpdate: boolean;
  canCreateLesson: boolean;
  canUpdateLesson: boolean;
  canDeleteLesson: boolean;
  canViewMaterials: boolean;
  canManageMaterials: boolean;
  onMove: (index: number, dir: -1 | 1) => void;
  onStructureChange: () => void;
}) {
  const [expanded, setExpanded] = useState(true);
  const [editing, setEditing] = useState(false);
  const [title, setTitle] = useState(module.title);
  const [description, setDescription] = useState(module.description ?? "");
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [addingLesson, setAddingLesson] = useState(false);
  const [newLessonTitle, setNewLessonTitle] = useState("");

  useEffect(() => {
    setTitle(module.title);
    setDescription(module.description ?? "");
  }, [module.id, module.title, module.description]);

  const lessons = [...module.lessons].sort((a, b) => a.sortOrder - b.sortOrder);

  const updateModuleMutation = useMutation({
    mutationFn: (input: { title: string; description: string | null }) =>
      updateCourseModule({ moduleId: module.id, ...input }),
    onSuccess: () => {
      toast.success("Раздел обновлён");
      setEditing(false);
      onStructureChange();
    },
    onError: (err) => toast.error("Не удалось обновить раздел", { description: describe(err) }),
  });

  const deleteModuleMutation = useMutation({
    mutationFn: () => deleteCourseModule(module.id),
    onSuccess: () => {
      toast.success("Раздел удалён");
      onStructureChange();
    },
    onError: (err) => toast.error("Не удалось удалить раздел", { description: describe(err) }),
  });

  const createLessonMutation = useMutation({
    mutationFn: (t: string) =>
      createLesson({ moduleId: module.id, title: t, durationMinutes: 45 }),
    onSuccess: () => {
      toast.success("Урок добавлен");
      setNewLessonTitle("");
      setAddingLesson(false);
      onStructureChange();
    },
    onError: (err) => toast.error("Не удалось добавить урок", { description: describe(err) }),
  });

  const reorderLessonsMutation = useMutation({
    mutationFn: (orderedLessonIds: string[]) =>
      reorderLessons({ moduleId: module.id, orderedLessonIds }),
    onSuccess: () => onStructureChange(),
    onError: (err) => {
      toast.error("Не удалось изменить порядок уроков", { description: describe(err) });
      onStructureChange();
    },
  });

  const moveLesson = (i: number, dir: -1 | 1) => {
    const ids = lessons.map((l) => l.id);
    const target = i + dir;
    if (target < 0 || target >= ids.length) return;
    [ids[i], ids[target]] = [ids[target], ids[i]];
    reorderLessonsMutation.mutate(ids);
  };

  return (
    <li className="px-3 py-3 sm:px-4">
      <div className="flex items-start gap-2">
        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          className="mt-0.5 grid size-6 shrink-0 place-items-center rounded text-[var(--color-muted-foreground)]"
          aria-label={expanded ? "Свернуть раздел" : "Развернуть раздел"}
        >
          {expanded ? <ChevronDown className="size-4" /> : <ChevronRight className="size-4" />}
        </button>

        <div className="min-w-0 flex-1">
          {editing ? (
            <form
              className="space-y-2"
              onSubmit={(e) => {
                e.preventDefault();
                const t = title.trim();
                if (t)
                  updateModuleMutation.mutate({
                    title: t,
                    description: description.trim() || null,
                  });
              }}
            >
              <Input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                aria-label="Название раздела"
                className="h-8 text-[13px]"
                autoFocus
              />
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={2}
                placeholder="Описание раздела (необязательно)"
                aria-label="Описание раздела"
                className={TEXTAREA_CLS}
              />
              <div className="flex gap-2">
                <Button type="submit" size="sm" disabled={updateModuleMutation.isPending}>
                  {updateModuleMutation.isPending ? "…" : "Сохранить"}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  onClick={() => {
                    setEditing(false);
                    setTitle(module.title);
                    setDescription(module.description ?? "");
                  }}
                >
                  Отмена
                </Button>
              </div>
            </form>
          ) : (
            <>
              <div className="flex items-center gap-2">
                <span className="text-[10px] font-mono text-[var(--color-muted-foreground)]">
                  {index + 1}.
                </span>
                <h3 className="truncate text-[14px] font-semibold text-[var(--color-foreground)]">
                  {module.title}
                </h3>
                <span className="shrink-0 text-[11px] text-[var(--color-muted-foreground)]">
                  {lessons.length} {lessons.length === 1 ? "урок" : "уроков"}
                </span>
              </div>
              {module.description && (
                <p className="mt-0.5 line-clamp-2 text-[12px] text-[var(--color-muted-foreground)]">
                  {module.description}
                </p>
              )}
            </>
          )}
        </div>

        {canUpdate && !editing && (
          <div className="flex shrink-0 items-center gap-0.5">
            <SmallIcon label="Раздел выше" onClick={() => onMove(index, -1)} disabled={index === 0}>
              <ArrowUp className="size-3.5" />
            </SmallIcon>
            <SmallIcon
              label="Раздел ниже"
              onClick={() => onMove(index, 1)}
              disabled={index === total - 1}
            >
              <ArrowDown className="size-3.5" />
            </SmallIcon>
            <SmallIcon label="Изменить раздел" onClick={() => setEditing(true)}>
              <Pencil className="size-3.5" />
            </SmallIcon>
            <SmallIcon label="Удалить раздел" onClick={() => setConfirmDelete(true)} danger>
              <Trash2 className="size-3.5" />
            </SmallIcon>
          </div>
        )}
      </div>

      {confirmDelete && (
        <div className="mt-2 flex flex-wrap items-center gap-2 rounded-lg bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-[12px] text-[var(--color-destructive)]">
          <AlertTriangle className="size-3.5 shrink-0" />
          <span>
            Удалить раздел «{module.title}»? Каскадно удалятся все его уроки и их
            материалы.
          </span>
          <div className="ml-auto flex gap-2">
            <Button
              size="sm"
              variant="outline"
              className="h-7 px-2 text-[11px]"
              onClick={() => setConfirmDelete(false)}
              disabled={deleteModuleMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              size="sm"
              variant="destructive"
              className="h-7 px-2 text-[11px]"
              onClick={() => deleteModuleMutation.mutate()}
              disabled={deleteModuleMutation.isPending}
            >
              {deleteModuleMutation.isPending ? "Удаление…" : "Удалить"}
            </Button>
          </div>
        </div>
      )}

      {expanded && (
        <div className="mt-3 space-y-2 pl-8">
          {lessons.map((lesson, i) => (
            <LessonCard
              key={lesson.id}
              lesson={lesson}
              index={i}
              total={lessons.length}
              canUpdate={canUpdateLesson}
              canDelete={canDeleteLesson}
              canViewMaterials={canViewMaterials}
              canManageMaterials={canManageMaterials}
              onMove={moveLesson}
              onDeleted={onStructureChange}
            />
          ))}

          {canCreateLesson && (
            <div>
              {addingLesson ? (
                <form
                  className="flex items-center gap-2"
                  onSubmit={(e) => {
                    e.preventDefault();
                    const t = newLessonTitle.trim();
                    if (t) createLessonMutation.mutate(t);
                  }}
                >
                  <Input
                    value={newLessonTitle}
                    onChange={(e) => setNewLessonTitle(e.target.value)}
                    placeholder="Название урока"
                    aria-label="Название урока"
                    autoFocus
                    className="h-8 flex-1 text-[13px]"
                  />
                  <Button type="submit" size="sm" disabled={createLessonMutation.isPending}>
                    {createLessonMutation.isPending ? "…" : "Добавить"}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      setAddingLesson(false);
                      setNewLessonTitle("");
                    }}
                    disabled={createLessonMutation.isPending}
                  >
                    Отмена
                  </Button>
                </form>
              ) : (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-7 gap-1.5 px-2 text-[12px] text-[var(--color-muted-foreground)]"
                  onClick={() => setAddingLesson(true)}
                >
                  <Plus className="size-3.5" />
                  Добавить урок
                </Button>
              )}
            </div>
          )}
        </div>
      )}
    </li>
  );
}

function SmallIcon({
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

// ───────────────────────────────────────────────────────────────────────
//  Lesson card — inline fields with debounced autosave (rule 9: the
//  payload is built at call time and passed through mutate(arg); the
//  success callback only reads `variables`, never component state).
// ───────────────────────────────────────────────────────────────────────

type SaveStatus = "idle" | "saving" | "saved" | "error";

function LessonCard({
  lesson,
  index,
  total,
  canUpdate,
  canDelete,
  canViewMaterials,
  canManageMaterials,
  onMove,
  onDeleted,
}: {
  lesson: LessonDto;
  index: number;
  total: number;
  canUpdate: boolean;
  canDelete: boolean;
  canViewMaterials: boolean;
  canManageMaterials: boolean;
  onMove: (index: number, dir: -1 | 1) => void;
  onDeleted: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [showMaterials, setShowMaterials] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const [title, setTitle] = useState(lesson.title);
  const [objectives, setObjectives] = useState(lesson.objectives ?? "");
  const [content, setContent] = useState(lesson.content ?? "");
  const [duration, setDuration] = useState(String(lesson.durationMinutes));
  const [status, setStatus] = useState<SaveStatus>("idle");

  // Snapshot of what the server currently holds — updated from the mutation's
  // `variables`, never re-read from render state.
  const savedRef = useRef({
    title: lesson.title,
    objectives: lesson.objectives ?? "",
    content: lesson.content ?? "",
    durationMinutes: lesson.durationMinutes,
  });
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Re-sync when a different lesson identity lands in this slot (e.g. reorder).
  useEffect(() => {
    setTitle(lesson.title);
    setObjectives(lesson.objectives ?? "");
    setContent(lesson.content ?? "");
    setDuration(String(lesson.durationMinutes));
    savedRef.current = {
      title: lesson.title,
      objectives: lesson.objectives ?? "",
      content: lesson.content ?? "",
      durationMinutes: lesson.durationMinutes,
    };
    setStatus("idle");
  }, [lesson.id]); // eslint-disable-line react-hooks/exhaustive-deps

  const mutation = useMutation({
    mutationFn: (input: UpdateLessonInput) => updateLesson(input),
    onSuccess: (_data, variables) => {
      savedRef.current = {
        title: variables.title,
        objectives: variables.objectives ?? "",
        content: variables.content ?? "",
        durationMinutes: variables.durationMinutes,
      };
      setStatus("saved");
    },
    onError: (err) => {
      setStatus("error");
      toast.error("Не удалось сохранить урок", { description: describe(err) });
    },
  });

  const flush = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    const trimmedTitle = title.trim();
    const mins = Number.parseInt(duration, 10);
    const payload: UpdateLessonInput = {
      lessonId: lesson.id,
      title: trimmedTitle || lesson.title,
      objectives: objectives.trim() || null,
      content: content.trim() || null,
      durationMinutes: Number.isNaN(mins) || mins < 0 ? 0 : mins,
    };
    const s = savedRef.current;
    const unchanged =
      payload.title === s.title &&
      (payload.objectives ?? "") === s.objectives &&
      (payload.content ?? "") === s.content &&
      payload.durationMinutes === s.durationMinutes;
    if (unchanged) return;
    setStatus("saving");
    mutation.mutate(payload);
  }, [title, objectives, content, duration, lesson.id, lesson.title, mutation]);

  // Debounced autosave on any field change while the card is open.
  useEffect(() => {
    if (!canUpdate || !open) return;
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(flush, 700);
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [title, objectives, content, duration, canUpdate, open, flush]);

  const deleteMutation = useMutation({
    mutationFn: () => deleteLesson(lesson.id),
    onSuccess: () => {
      toast.success("Урок удалён");
      onDeleted();
    },
    onError: (err) => toast.error("Не удалось удалить урок", { description: describe(err) }),
  });

  const readOnly = !canUpdate;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)]">
      <div className="flex items-center gap-2 px-3 py-2">
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className="grid size-6 shrink-0 place-items-center rounded text-[var(--color-muted-foreground)]"
          aria-label={open ? "Свернуть урок" : "Развернуть урок"}
        >
          {open ? <ChevronDown className="size-3.5" /> : <ChevronRight className="size-3.5" />}
        </button>
        <span className="text-[10px] font-mono text-[var(--color-muted-foreground)]">
          {index + 1}
        </span>
        <span className="min-w-0 flex-1 truncate text-[13px] font-medium text-[var(--color-foreground)]">
          {title || "Без названия"}
        </span>
        <span className="hidden items-center gap-1 text-[11px] text-[var(--color-muted-foreground)] sm:flex">
          <Clock className="size-3" />
          {lesson.durationMinutes} мин
        </span>
        {status !== "idle" && (
          <span
            className={cn(
              "text-[10px] font-semibold uppercase tracking-wide",
              status === "saving" && "text-[var(--color-muted-foreground)]",
              status === "saved" && "text-[var(--color-success)]",
              status === "error" && "text-[var(--color-destructive)]",
            )}
          >
            {status === "saving" ? "Сохранение…" : status === "saved" ? "Сохранено" : "Ошибка"}
          </span>
        )}
        {canUpdate && (
          <div className="flex shrink-0 items-center gap-0.5">
            <SmallIcon label="Урок выше" onClick={() => onMove(index, -1)} disabled={index === 0}>
              <ArrowUp className="size-3.5" />
            </SmallIcon>
            <SmallIcon
              label="Урок ниже"
              onClick={() => onMove(index, 1)}
              disabled={index === total - 1}
            >
              <ArrowDown className="size-3.5" />
            </SmallIcon>
          </div>
        )}
        {canDelete && (
          <SmallIcon label={`Удалить урок ${title}`} onClick={() => setConfirmDelete(true)} danger>
            <Trash2 className="size-3.5" />
          </SmallIcon>
        )}
      </div>

      {confirmDelete && (
        <div className="flex flex-wrap items-center gap-2 border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-[12px] text-[var(--color-destructive)]">
          <span>Удалить урок «{title}»? Каскадно удалятся его материалы.</span>
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

      {open && (
        <div className="space-y-3 border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-3 py-3">
          <div className="grid gap-3 sm:grid-cols-[1fr_140px]">
            <Field id={`l-title-${lesson.id}`} label="Название урока" required>
              <Input
                id={`l-title-${lesson.id}`}
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                onBlur={flush}
                readOnly={readOnly}
                className="h-8 text-[13px]"
              />
            </Field>
            <Field id={`l-dur-${lesson.id}`} label="Минут">
              <Input
                id={`l-dur-${lesson.id}`}
                type="number"
                min="0"
                value={duration}
                onChange={(e) => setDuration(e.target.value)}
                onBlur={flush}
                readOnly={readOnly}
                className="h-8 tabular-nums text-[13px]"
              />
            </Field>
          </div>
          <Field id={`l-obj-${lesson.id}`} label="Цели">
            <textarea
              id={`l-obj-${lesson.id}`}
              value={objectives}
              onChange={(e) => setObjectives(e.target.value)}
              onBlur={flush}
              readOnly={readOnly}
              rows={2}
              className={TEXTAREA_CLS}
            />
          </Field>
          <Field id={`l-content-${lesson.id}`} label="Содержание" hint="Поддерживается markdown.">
            <textarea
              id={`l-content-${lesson.id}`}
              value={content}
              onChange={(e) => setContent(e.target.value)}
              onBlur={flush}
              readOnly={readOnly}
              rows={4}
              className={TEXTAREA_CLS}
            />
          </Field>

          {canViewMaterials && (
            <div className="rounded-lg border border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.3)] p-3">
              <button
                type="button"
                onClick={() => setShowMaterials((v) => !v)}
                className="flex w-full items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
              >
                {showMaterials ? (
                  <ChevronDown className="size-3.5" />
                ) : (
                  <ChevronRight className="size-3.5" />
                )}
                Материалы урока
              </button>
              {showMaterials && (
                <div className="mt-3">
                  <LessonMaterialsPanel
                    lessonId={lesson.id}
                    canManage={canManageMaterials}
                  />
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Course card edit dialog
// ───────────────────────────────────────────────────────────────────────

function CourseEditDialog({
  open,
  course,
  subjectOptions,
  onClose,
  onSaved,
}: {
  open: boolean;
  course: CourseDetailDto;
  subjectOptions: { value: string; label: string }[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const [title, setTitle] = useState(course.title);
  const [description, setDescription] = useState(course.description ?? "");
  const [level, setLevel] = useState<CourseLevel>(course.level);
  const [durationHours, setDurationHours] = useState(String(course.durationHours));
  const [subjectId, setSubjectId] = useState<string | null>(course.subjectId);

  useEffect(() => {
    if (open) {
      setTitle(course.title);
      setDescription(course.description ?? "");
      setLevel(course.level);
      setDurationHours(String(course.durationHours));
      setSubjectId(course.subjectId);
    }
  }, [open, course]);

  const mutation = useMutation({
    mutationFn: (input: UpdateCourseInput) => updateCourse(input),
    onSuccess: () => {
      toast.success("Курс обновлён");
      onSaved();
      onClose();
    },
    onError: (err) => toast.error("Не удалось обновить курс", { description: describe(err) }),
  });

  const hours = Number.parseInt(durationHours, 10);
  const valid =
    title.trim().length > 0 && !!subjectId && !Number.isNaN(hours) && hours >= 0;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid || !subjectId) return;
    mutation.mutate({
      courseId: course.id,
      subjectId,
      title: title.trim(),
      description: description.trim() || null,
      level,
      durationHours: hours,
      coverFileId: course.coverFileId ?? null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Карточка курса</DialogTitle>
            <DialogDescription>
              Основные параметры курса. Структура редактируется ниже, на странице.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="ce-title" label="Название" required>
              <Input
                id="ce-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
                autoFocus
              />
            </Field>
            <Field id="ce-subject" label="Направление" required>
              <Combobox
                id="ce-subject"
                label="Направление"
                value={subjectId}
                onChange={setSubjectId}
                options={subjectOptions}
                searchable
                required
              />
            </Field>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="ce-level" label="Уровень" required>
                <Combobox
                  id="ce-level"
                  label="Уровень"
                  value={level}
                  onChange={(v) => setLevel((v as CourseLevel) ?? course.level)}
                  options={COURSE_LEVELS.map((l) => ({ value: l, label: LEVEL_LABEL[l] }))}
                />
              </Field>
              <Field id="ce-hours" label="Длительность, часов" required>
                <Input
                  id="ce-hours"
                  type="number"
                  min="0"
                  value={durationHours}
                  onChange={(e) => setDurationHours(e.target.value)}
                  required
                  className="tabular-nums"
                />
              </Field>
            </div>
            <Field id="ce-desc" label="Описание">
              <textarea
                id="ce-desc"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                maxLength={4000}
                className={TEXTAREA_CLS}
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !valid}>
              {mutation.isPending ? "Сохранение…" : "Сохранить"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
