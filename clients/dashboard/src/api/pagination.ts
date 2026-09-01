/**
 * Shared page envelope — every list endpoint returns this shape
 * (`PagedResponse<T>` on the backend, camelCase over the wire). Lives in its
 * own module so api modules can import it without depending on any one
 * feature's api file.
 */
export type PagedResponse<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
};
