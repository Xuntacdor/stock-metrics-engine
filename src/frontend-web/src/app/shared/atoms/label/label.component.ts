import {
    Component, input, computed,
    ChangeDetectionStrategy, booleanAttribute,
} from '@angular/core';

@Component({
    selector: 'app-label',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <label
      [for]="forId()"
      [class]="labelClasses()"
    >
      <ng-content />

      @if (required()) {
        <span class="text-down ml-0.5" aria-hidden="true">*</span>
      }

      @if (optional()) {
        <span class="text-fg-muted text-xs font-normal ml-1">(tuỳ chọn)</span>
      }
    </label>
  `,
})
export class LabelComponent {
    readonly forId = input<string>('', { alias: 'for' });
    readonly required = input(false, { transform: booleanAttribute });
    readonly optional = input(false, { transform: booleanAttribute });
    readonly size = input<'sm' | 'md'>('md');
    readonly muted = input(false, { transform: booleanAttribute });

    readonly labelClasses = computed(() => [
        'block font-medium',
        this.size() === 'sm' ? 'text-xs' : 'text-small',
        this.muted() ? 'text-fg-muted' : 'text-fg',
        'mb-1.5 select-none',
    ].join(' '));
}
