import type { HTMLAttributes, ReactNode } from 'react';

export type BadgeVariant =
  | 'default'
  | 'published'
  | 'draft'
  | 'active'
  | 'pending'
  | 'deactivated'
  | 'valid'
  | 'expired'
  | 'role'
  | 'capability'
  | 'warning';

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
  children: ReactNode;
}

export function Badge({ variant = 'default', children, className = '', ...props }: BadgeProps) {
  return (
    <span className={`badge-base badge-${variant} ${className}`.trim()} {...props}>
      {children}
    </span>
  );
}

export function StatusBadge({ status, className = '' }: { status: string; className?: string }) {
  const normalized = status.toLowerCase();
  const variant: BadgeVariant =
    normalized === 'published' || normalized === 'active' || normalized === 'valid'
      ? 'published'
      : normalized === 'draft' || normalized === 'pending'
        ? 'draft'
        : normalized === 'deactivated' || normalized === 'expired'
          ? 'deactivated'
          : 'default';

  return (
    <span className={`status ${normalized} badge-base badge-${variant} ${className}`.trim()}>
      {status}
    </span>
  );
}
