import {
  Component, input, output, model, computed,
  ChangeDetectionStrategy, booleanAttribute,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'search';
export type InputState = 'default' | 'error' | 'success' | 'disabled';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative flex items-center">
      <!-- Left icon -->
      @if (iconLeft()) {
        <span class="absolute left-3 text-fg-muted pointer-events-none" aria-hidden="true">
          <ng-content select="[slot=icon-left]" />
        </span>
      }

      <input
        [type]="showPassword() ? 'text' : type()"
        [id]="inputId()"
        [name]="name()"
        [placeholder]="placeholder()"
        [disabled]="state() === 'disabled' || disabled()"
        [attr.aria-invalid]="state() === 'error'"
        [attr.aria-describedby]="errorId() || null"
        [attr.autocomplete]="autocomplete()"
        [attr.inputmode]="inputMode()"
        [attr.min]="min()"
        [attr.max]="max()"
        [attr.step]="step()"
        [attr.maxlength]="maxlength()"
        [(ngModel)]="value"
        [class]="inputClasses()"
        (blur)="onBlur()"
        (focus)="onFocus()"
      />

      <!-- Password toggle -->
      @if (type() === 'password') {
        <button
          type="button"
          class="absolute right-3 text-fg-muted hover:text-fg transition-colors"
          [attr.aria-label]="showPassword() ? 'Ẩn mật khẩu' : 'Hiển thị mật khẩu'"
          (click)="togglePassword()"
        >
          @if (showPassword()) {
            <!-- Eye-off icon -->
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
            </svg>
          } @else {
            <!-- Eye icon -->
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
            </svg>
          }
        </button>
      }

      <!-- Clear button for search -->
      @if (type() === 'search' && value()) {
        <button
          type="button"
          class="absolute right-3 text-fg-muted hover:text-fg transition-colors"
          aria-label="Xóa tìm kiếm"
          (click)="clearValue()"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>
      }
    </div>
  `,
})
export class InputComponent {
  readonly value = model<string | number>('');


  readonly type = input<InputType>('text');
  readonly state = input<InputState>('default');
  readonly inputId = input<string>('');
  readonly name = input<string>('');
  readonly placeholder = input<string>('');
  readonly autocomplete = input<string>('');
  readonly inputMode = input<string>('');
  readonly min = input<number | string | undefined>(undefined);
  readonly max = input<number | string | undefined>(undefined);
  readonly step = input<number | string | undefined>(undefined);
  readonly maxlength = input<number | undefined>(undefined);
  readonly iconLeft = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly errorId = input<string>('');


  readonly blurred = output<void>();
  readonly focused = output<void>();
  readonly cleared = output<void>();


  protected showPassword = model(false);

  readonly inputClasses = computed(() => {
    const base = [
      'w-full h-10 rounded-md text-body transition-all duration-150',
      'bg-surface-2 border text-fg',
      'placeholder:text-fg-muted',
      'focus:outline-none focus:ring-2',
      this.iconLeft() ? 'pl-9' : 'pl-3',
      this.type() === 'password' || this.type() === 'search' ? 'pr-10' : 'pr-3',
    ];

    const states: Record<InputState, string> = {
      default: 'border-border hover:border-border-hover focus:ring-up/40 focus:border-up',
      error: 'border-down hover:border-down focus:ring-down/40 focus:border-down',
      success: 'border-up hover:border-up/70 focus:ring-up/30 focus:border-up',
      disabled: 'border-border opacity-40 cursor-not-allowed',
    };

    return [...base, states[this.state()]].join(' ');
  });

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  clearValue(): void {
    this.value.set('');
    this.cleared.emit();
  }

  onBlur(): void { this.blurred.emit(); }
  onFocus(): void { this.focused.emit(); }
}
