import {
    Component, input, output, computed,
    ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface TabItem {
    id: string;
    label: string;
    icon?: string;
    badge?: string | number;
    disabled?: boolean;
}

export type TabNavVariant = 'underline' | 'pills' | 'bordered';


@Component({
    selector: 'app-tab-nav',
    standalone: true,
    imports: [CommonModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <nav
      [class]="navClasses()"
      role="tablist"
      [attr.aria-label]="ariaLabel()"
    >
      @for (tab of tabs(); track tab.id) {
        <button
          role="tab"
          [id]="'tab-' + tab.id"
          [attr.aria-selected]="tab.id === activeId()"
          [attr.aria-controls]="'panel-' + tab.id"
          [disabled]="tab.disabled"
          [class]="getTabClasses(tab.id, tab.disabled)"
          (click)="selectTab(tab)"
          (keydown.arrowRight)="navigateTab(1)"
          (keydown.arrowLeft)="navigateTab(-1)"
        >
          <!-- Label -->
          <span>{{ tab.label }}</span>

          <!-- Badge count (only show numeric badges) -->
          @if (tab.badge !== undefined && tab.badge !== '') {
            <span [class]="getBadgeClasses(tab.id)">
              {{ tab.badge }}
            </span>
          }
        </button>
      }
    </nav>
  `,
})
export class TabNavComponent {
    readonly tabs = input.required<TabItem[]>();
    readonly activeId = input<string>('');
    readonly variant = input<TabNavVariant>('underline');
    readonly ariaLabel = input<string>('Điều hướng tab');
    readonly fullWidth = input(false);

    readonly tabSelected = output<TabItem>();
    readonly activeIdChange = output<string>();

    readonly navClasses = computed(() => {
        const base = 'flex items-center';
        const variants: Record<TabNavVariant, string> = {
            underline: 'gap-0 border-b border-border',
            pills: 'gap-1 p-1 bg-surface-2 rounded-lg',
            bordered: 'gap-0 border border-border rounded-lg overflow-hidden divide-x divide-border',
        };
        return [base, variants[this.variant()], this.fullWidth() ? 'w-full' : ''].filter(Boolean).join(' ');
    });

    getTabClasses(id: string, disabled?: boolean): string {
        const isActive = id === this.activeId();
        const base = [
            'relative flex items-center gap-1.5 px-4 py-2.5 text-small font-medium',
            'transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-up/50',
            'whitespace-nowrap select-none',
            disabled ? 'opacity-40 cursor-not-allowed pointer-events-none' : 'cursor-pointer',
        ];

        const variantStyles: Record<TabNavVariant, { active: string; inactive: string }> = {
            underline: {
                active: 'text-fg border-b-2 border-up -mb-px',
                inactive: 'text-fg-muted hover:text-fg hover:bg-surface-2 rounded-t-md border-b-2 border-transparent -mb-px',
            },
            pills: {
                active: 'text-fg bg-surface rounded-md shadow-sm',
                inactive: 'text-fg-muted hover:text-fg hover:bg-surface/60 rounded-md',
            },
            bordered: {
                active: 'text-fg bg-surface-2',
                inactive: 'text-fg-muted hover:text-fg hover:bg-surface',
            },
        };

        const style = variantStyles[this.variant()];
        return [...base, isActive ? style.active : style.inactive].join(' ');
    }

    getBadgeClasses(id: string): string {
        const isActive = id === this.activeId();
        return [
            'inline-flex items-center justify-center min-w-[1.25rem] h-5 px-1.5',
            'rounded-full text-xs font-semibold',
            isActive ? 'bg-up/20 text-up' : 'bg-surface-2 text-fg-muted',
        ].join(' ');
    }

    selectTab(tab: TabItem): void {
        if (!tab.disabled) {
            this.tabSelected.emit(tab);
            this.activeIdChange.emit(tab.id);
        }
    }

    navigateTab(direction: 1 | -1): void {
        const tabs = this.tabs().filter(t => !t.disabled);
        const currentIdx = tabs.findIndex(t => t.id === this.activeId());
        const nextIdx = (currentIdx + direction + tabs.length) % tabs.length;
        this.selectTab(tabs[nextIdx]);
    }
}
