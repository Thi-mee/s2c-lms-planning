import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react';

export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: ReactNode;
  description?: ReactNode;
  cardStyle?: boolean;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(function Checkbox(
  { label, description, cardStyle = false, checked, className = '', id, ...props },
  ref
) {
  const containerClass = cardStyle
    ? `checkbox-card ${checked ? 'checkbox-card-checked' : ''} ${className}`.trim()
    : `checkbox-control ${className}`.trim();

  return (
    <label className={containerClass} htmlFor={id}>
      <span className="checkbox-input-wrap">
        <input
          ref={ref}
          id={id}
          type="checkbox"
          checked={checked}
          className="checkbox-input"
          {...props}
        />
        <span className="checkbox-box" aria-hidden="true">
          <svg className="checkbox-check-icon" viewBox="0 0 16 16" fill="none">
            <path
              d="M3.5 8.5L6.5 11.5L12.5 4.5"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </span>
      </span>
      <span className="checkbox-label-content">
        <span className="checkbox-label-title">{label}</span>
        {description && <span className="checkbox-label-desc">{description}</span>}
      </span>
    </label>
  );
});
