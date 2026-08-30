import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, FileUp, GraduationCap, Upload, XCircle } from "lucide-react";
import { toast } from "sonner";
import { importStudents, type ImportStudentsResultDto } from "@/api/people";
import { Button } from "@/components/ui/button";
import {
  EntityDetailBack,
  EntityDetailSection,
  EntityPageHeader,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

// CSV columns expected by the backend (header row required):
// LastName,FirstName,MiddleName,BirthDate,Phone,Email,ManagerUserId,Source
const CSV_TEMPLATE =
  "LastName,FirstName,MiddleName,BirthDate,Phone,Email,ManagerUserId,Source";

export function StudentImportPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<ImportStudentsResultDto | null>(null);

  const importMut = useMutation({
    mutationFn: (vars: { file: File; dryRun: boolean }) => importStudents(vars.file, vars.dryRun),
    onSuccess: (result, vars) => {
      if (vars.dryRun) {
        setPreview(result);
      } else {
        toast.success(`Импортировано учеников: ${result.successCount}`, {
          description: result.errorCount > 0 ? `Пропущено с ошибками: ${result.errorCount}` : undefined,
        });
        void queryClient.invalidateQueries({ queryKey: ["students"] });
        navigate("/students");
      }
    },
    onError: (e) => toast.error("Ошибка импорта", { description: describe(e) }),
  });

  const onPickFile = (f: File | null) => {
    setFile(f);
    setPreview(null);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <div>
        <EntityDetailBack to="/students" label="К списку учеников" />
        <EntityPageHeader
          icon={GraduationCap}
          title="Импорт учеников из CSV"
          description="Сначала выполняется предпросмотр без записи. Строки с ошибками не блокируют остальные."
        />
      </div>

      <EntityDetailSection title="Файл" icon={FileUp}>
        <div className="space-y-3">
          <div
            className={cn(
              "flex items-center justify-between gap-3 rounded-lg border border-dashed border-[var(--color-border)] px-4 py-3",
            )}
          >
            <div className="min-w-0 text-[13px]">
              {file ? (
                <>
                  <p className="truncate font-medium text-[var(--color-foreground)]">{file.name}</p>
                  <p className="text-[11.5px] text-[var(--color-muted-foreground)]">
                    {(file.size / 1024).toFixed(1)} КБ
                  </p>
                </>
              ) : (
                <p className="text-[var(--color-muted-foreground)]">Файл не выбран</p>
              )}
            </div>
            <input
              ref={inputRef}
              type="file"
              accept=".csv,text/csv"
              className="hidden"
              onChange={(e) => onPickFile(e.target.files?.[0] ?? null)}
            />
            <Button variant="outline" size="sm" className="gap-1.5 shrink-0" onClick={() => inputRef.current?.click()}>
              <Upload className="size-3.5" />
              Выбрать файл
            </Button>
          </div>

          <p className="text-[11.5px] text-[var(--color-muted-foreground)]">
            Ожидаемые колонки (первая строка — заголовок):{" "}
            <code className="text-[11px]">{CSV_TEMPLATE}</code>. Поля{" "}
            <code>MiddleName</code>/<code>Source</code> можно оставить пустыми,{" "}
            <code>BirthDate</code> в формате <code>yyyy-MM-dd</code>.
          </p>

          <div className="flex flex-wrap gap-2">
            <Button
              className="gap-1.5"
              disabled={!file || importMut.isPending}
              onClick={() => file && importMut.mutate({ file, dryRun: true })}
            >
              <FileUp className="size-4" />
              {importMut.isPending && importMut.variables?.dryRun ? "Проверка…" : "Предпросмотр"}
            </Button>
            <Button
              variant="outline"
              className="gap-1.5"
              disabled={!file || !preview || preview.successCount === 0 || importMut.isPending}
              onClick={() => file && importMut.mutate({ file, dryRun: false })}
            >
              <Upload className="size-4" />
              {importMut.isPending && importMut.variables?.dryRun === false
                ? "Импорт…"
                : `Импортировать${preview ? ` (${preview.successCount})` : ""}`}
            </Button>
          </div>
        </div>
      </EntityDetailSection>

      {preview && (
        <EntityDetailSection
          title="Результат предпросмотра"
          description={`Всего строк: ${preview.totalRows} · успешно: ${preview.successCount} · с ошибками: ${preview.errorCount}`}
        >
          <div className="overflow-x-auto">
            <table className="w-full min-w-[480px] text-[12.5px]">
              <thead>
                <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  <th className="py-2 pr-3 font-semibold">Строка</th>
                  <th className="py-2 pr-3 font-semibold">Статус</th>
                  <th className="py-2 font-semibold">Сообщение</th>
                </tr>
              </thead>
              <tbody>
                {preview.rows.map((row) => (
                  <tr
                    key={row.rowNumber}
                    className="border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] last:border-0"
                  >
                    <td className="py-2 pr-3 tabular-nums text-[var(--color-muted-foreground)]">{row.rowNumber}</td>
                    <td className="py-2 pr-3">
                      {row.success ? (
                        <span className="inline-flex items-center gap-1 text-[var(--color-success)]">
                          <CheckCircle2 className="size-3.5" /> ОК
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-[var(--color-destructive)]">
                          <XCircle className="size-3.5" /> Ошибка
                        </span>
                      )}
                    </td>
                    <td className="py-2 text-[var(--color-foreground)]">{row.error || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </EntityDetailSection>
      )}
    </div>
  );
}
