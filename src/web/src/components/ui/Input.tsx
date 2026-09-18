import { forwardRef, type InputHTMLAttributes, type TextareaHTMLAttributes, type ReactNode } from 'react';

export interface FormFieldWrapperProps {
  id?: string;
  label?: ReactNode;
  helpText?: ReactNode;
  error?: ReactNode;
  required?: boolean;
  className?: string;
  children: ReactNode;
}

export function FormFieldWrapper({
  id,
  label,
  helpText,
  error,
  required,
  className = '',
  children,
}: FormFieldWrapperProps) {
  return (
    <div className={`form-field-group ${error ? 'has-error' : ''} ${className}`.trim()}>
      {label && (
        <label htmlFor={id} className="field-label">
          {label}
        </label>
      )}
      {children}
      {helpText && !error && <p className="field-help">{helpText}</p>}
      {error && <p className="field-error" role="alert">{error}</p>}
    </div>
  );
}

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: ReactNode;
  helpText?: ReactNode;
  error?: ReactNode;
  wrapperClassName?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { label, helpText, error, id, required, className = '', wrapperClassName = '', ...props },
  ref
) {
  const inputEl = (
    <input
      ref={ref}
      id={id}
      required={required}
      aria-invalid={Boolean(error)}
      aria-describedby={
        error && id ? `${id}-error` : helpText && id ? `${id}-help` : undefined
      }
      className={`input-base ${error ? 'input-error' : ''} ${className}`.trim()}
      {...props}
    />
  );

  if (!label && !helpText && !error) {
    return inputEl;
  }

  return (
    <FormFieldWrapper
      id={id}
      label={label}
      helpText={helpText}
      error={error}
      required={required}
      className={wrapperClassName}
    >
      {inputEl}
    </FormFieldWrapper>
  );
});

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: ReactNode;
  helpText?: ReactNode;
  error?: ReactNode;
  monospace?: boolean;
  wrapperClassName?: string;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(function Textarea(
  { label, helpText, error, id, required, monospace, className = '', wrapperClassName = '', ...props },
  ref
) {
  const textareaEl = (
    <textarea
      ref={ref}
      id={id}
      required={required}
      aria-invalid={Boolean(error)}
      aria-describedby={
        error && id ? `${id}-error` : helpText && id ? `${id}-help` : undefined
      }
      className={`textarea-base ${monospace ? 'font-mono' : ''} ${error ? 'input-error' : ''} ${className}`.trim()}
      {...props}
    />
  );

  if (!label && !helpText && !error) {
    return textareaEl;
  }

  return (
    <FormFieldWrapper
      id={id}
      label={label}
      helpText={helpText}
      error={error}
      required={required}
      className={wrapperClassName}
    >
      {textareaEl}
    </FormFieldWrapper>
  );
});
