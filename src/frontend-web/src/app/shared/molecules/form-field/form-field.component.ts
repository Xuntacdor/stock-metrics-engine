import {
  Component, input, computed,
  ChangeDetectionStrategy, booleanAttribute,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LabelComponent } from '../../atoms/label/label.component';


@Component({
  selector: 'app-form-field',
  standalone: true,
  imports: [CommonModule, LabelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="wrapperClasses()">

      <!-- Label -->
      @if (label()) {
        <app-label
          [for]="fieldId()"
          [required]="required()"
          [optional]="optional()"
        >{{ label() }}</app-label>
      }

      <!-- Input slot -->
      <div class="relative">
        <ng-content />
      </div>

      <!-- Error message -->
      @if (errorMessage() && hasError()) {
        <p
          [id]="fieldId() + '-error'"
          class="mt-1.5 text-xs text-down flex items-center gap-1 animate-fade-in"
          role="alert"
          aria-live="polite"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
            <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
          {{ errorMessage() }}
        </p>
      }

      <!-- Hint message (shown when no error) -->
      @if (hintMessage() && !hasError()) {
        <p class="mt-1.5 text-xs text-fg-muted">
          {{ hintMessage() }}
        </p>
      }

      <!-- Character count -->
      @if (showCount() && maxLength()) {
        <p class="mt-1 text-xs text-fg-muted text-right">
          {{ currentLength() }} / {{ maxLength() }}
        </p>
      }
    </div>
  `,
})
export class FormFieldComponent {
  readonly label = input<string>('');
  readonly fieldId = input<string>('');
  readonly required = input(false, { transform: booleanAttribute });
  readonly optional = input(false, { transform: booleanAttribute });
  readonly hasError = input(false, { transform: booleanAttribute });
  readonly errorMessage = input<string>('');
  readonly hintMessage = input<string>('');
  readonly showCount = input(false, { transform: booleanAttribute });
  readonly maxLength = input<number>(0);
  readonly currentLength = input<number>(0);
  readonly stacked = input(true, { transform: booleanAttribute });

  readonly wrapperClasses = computed(() => [
    'flex',
    this.stacked() ? 'flex-col' : 'flex-row items-center gap-4',
  ].join(' '));
}
