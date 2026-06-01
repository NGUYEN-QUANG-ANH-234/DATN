import type { ReactNode } from "react";
import { Search } from "lucide-react";
import { Button } from "./Button";
import { Input } from "./Input";

export type FilterBarProps = {
  searchValue?: string;
  searchPlaceholder?: string;
  onSearchChange?: (value: string) => void;
  filters?: ReactNode;
  actions?: ReactNode;
  onReset?: () => void;
};

export const FilterBar = ({
  searchValue,
  searchPlaceholder = "Tìm kiếm...",
  onSearchChange,
  filters,
  actions,
  onReset,
}: FilterBarProps) => (
  <div className="hicas-card hicas-card-padded">
    <div className="grid gap-3 lg:grid-cols-[minmax(260px,1fr)_auto_auto] lg:items-end">
      {onSearchChange && (
        <Input
          value={searchValue ?? ""}
          onChange={(event) => onSearchChange(event.target.value)}
          placeholder={searchPlaceholder}
          iconLeft={<Search size={16} />}
          aria-label={searchPlaceholder}
        />
      )}
      {filters && <div className="grid gap-3 sm:grid-cols-2 lg:flex lg:items-end">{filters}</div>}
      <div className="flex flex-wrap items-center gap-2">
        {onReset && (
          <Button variant="secondary" onClick={onReset}>
            Đặt lại
          </Button>
        )}
        {actions}
      </div>
    </div>
  </div>
);
