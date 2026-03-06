import {
  Component, input, computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';

export type SkeletonType = 'text' | 'rect' | 'circle' | 'price' | 'card';
export type SkeletonWidth = 'sm' | 'md' | 'lg' | 'full' | string;
export type SkeletonHeight = 'sm' | 'md' | 'lg' | string;

@Component({
  selector: 'app-skeleton',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Single skeleton element -->
    @if (type() !== 'card') {
      <span
        [class]="skeletonClasses()"
        [style.width]="customWidth()"
        [style.height]="customHeight()"
        role="status"
        aria-label="Đang tải..."
        aria-live="polite"
      ></span>
    }

    <!-- Card skeleton (composite) -->
    @if (type() === 'card') {
      <div class="card space-y-3" role="status" aria-label="Đang tải...">
        <!-- Header row -->
        <div class="flex items-center gap-3">
          <span class="skeleton-base rounded-full w-10 h-10 shrink-0"></span>
          <div class="flex-1 space-y-2">
            <span class="skeleton-base block h-4 rounded w-1/3"></span>
            <span class="skeleton-base block h-3 rounded w-1/2"></span>
          </div>
        </div>
        <!-- Content lines -->
        <span class="skeleton-base block h-3 rounded w-full"></span>
        <span class="skeleton-base block h-3 rounded w-5/6"></span>
        <span class="skeleton-base block h-3 rounded w-4/6"></span>
        <!-- Footer -->
        <div class="flex gap-2 pt-1">
          <span class="skeleton-base block h-8 rounded w-24"></span>
          <span class="skeleton-base block h-8 rounded w-16"></span>
        </div>
        <span class="sr-only">Đang tải nội dung...</span>
      </div>
    }
  `,
})
export class SkeletonComponent {
  readonly type = input<SkeletonType>('text');
  readonly width = input<SkeletonWidth>('md');
  readonly height = input<SkeletonHeight>('md');
  readonly lines = input<number>(1);
  readonly rounded = input(true);

  readonly skeletonClasses = computed(() => {
    const base = [
      'skeleton-base block',
      this.rounded() ? 'rounded' : '',
    ];

    const typeShapes: Partial<Record<SkeletonType, string>> = {
      text: '',
      rect: '',
      circle: 'rounded-full',
      price: 'rounded font-numeric',
    };

    const widthMap: Record<string, string> = {
      sm: 'w-16',
      md: 'w-32',
      lg: 'w-48',
      full: 'w-full',
    };

    const heightMap: Record<string, string> = {
      sm: 'h-3',
      md: 'h-4',
      lg: 'h-6',
    };

    const w = widthMap[this.width()] ?? '';
    const h = heightMap[this.height()] ?? '';

    return [
      ...base,
      typeShapes[this.type()] ?? '',
      w,
      h,
    ].filter(Boolean).join(' ');
  });

  readonly customWidth = computed(() => {
    const w = this.width();
    if (this.type() === 'circle') return undefined;
    return ['sm', 'md', 'lg', 'full'].includes(w) ? undefined : w;
  });

  readonly customHeight = computed(() => {
    const h = this.height();
    if (this.type() === 'circle') return this.width() as string;
    return ['sm', 'md', 'lg'].includes(h) ? undefined : h;
  });
}
