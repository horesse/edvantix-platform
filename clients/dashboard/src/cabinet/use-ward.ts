import { useContext } from "react";
import { WardContext } from "./ward-context";

/** Доступ к переключателю подопечных представителя. Вне `WardProvider`
 *  отдаёт пустое состояние (`hasWards === false`), так что экраны кабинета
 *  безопасно вызывают хук всегда. */
export function useWard() {
  return useContext(WardContext);
}
