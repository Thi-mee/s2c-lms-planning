import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'quiet' | 'text' | 'danger' | 'add';
  size?: 'sm' | 'md' | 'lg';
  loading?: boolean;
  icon?: ReactNode;
  iconRight?: ReactNode;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    children,
    className = '',
    variant = 'secondary',
    size = 'md',
    loading = false,
    disabled,
    icon,
    iconRight,
    type = 'button',
    ...props
  },
  ref
) {
  const variantClass =
    variant === 'primary'
      ? 'primary-button'
      : variant === 'quiet'
        ? 'quiet-button'
        : variant === 'text'
          ? 'text-button'
          : variant === 'add'
            ? 'add-button'
            : variant === 'danger'
              ? 'danger-button'
              : 'secondary-button';

  const sizeClass = size === 'sm' ? 'btn-sm' : size === 'lg' ? 'btn-lg' : '';

  return (
    <button
      ref={ref}
      type={type}
      className={`btn-base ${variantClass} ${sizeClass} ${className}`.trim()}
      disabled={disabled || loading}
      {...props}
    >
      {loading ? (
        <span className="btn-spinner" aria-hidden="true" />
      ) : icon ? (
        <span className="btn-icon" aria-hidden="true">
          {icon}
        </span>
      ) : null}
      <span className="btn-content">{children}</span>
      {iconRight && !loading && (
        <span className="btn-icon-right" aria-hidden="true">
          {iconRight}
        </span>
      )}
    </button>
  );
});
