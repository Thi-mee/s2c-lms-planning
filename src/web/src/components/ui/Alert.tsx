import type { ReactNode } from 'react';

export interface AlertProps {
  type: 'error' | 'success' | 'warning' | 'info';
  children: ReactNode;
  className?: string;
}

export function Alert({ type, children, className = '' }: AlertProps) {
  const role = type === 'error' ? 'alert' : 'status';

  return (
    <div
      role={role}
      className={`alert-banner alert-${type} ${type === 'error' ? 'error' : type === 'success' ? 'success' : ''} ${className}`.trim()}
    >
      <div className="alert-content">{children}</div>
    </div>
  );
}
