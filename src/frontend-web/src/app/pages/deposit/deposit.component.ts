import {
  Component, signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { CardComponent } from '../../shared/molecules/card/card.component';
import { TabNavComponent, type TabItem } from '../../shared/molecules/tab-nav/tab-nav.component';
import { FormFieldComponent } from '../../shared/molecules/form-field/form-field.component';
import { StatBoxComponent, type StatBoxData } from '../../shared/molecules/stat-box/stat-box.component';
import { BadgeComponent } from '../../shared/atoms/badge/badge.component';
import { ButtonComponent } from '../../shared/atoms/button/button.component';
import { InputComponent } from '../../shared/atoms/input/input.component';
import { IconComponent } from '../../shared/atoms/icon/icon.component';
import { WalletService } from '../../core/services/wallet.service';
import { PaymentService } from '../../core/services/payment.service';
import { TransactionService } from '../../core/services/transaction.service';
import { ToastService } from '../../core/services/toast.service';
import { inject, OnInit } from '@angular/core';

type TransactionType = 'deposit' | 'withdraw';

interface Transaction {
  id: string; date: string; type: TransactionType;
  amount: number; status: 'success' | 'pending' | 'failed';
  method: string; ref: string;
}

@Component({
  selector: 'app-deposit',
  standalone: true,
  imports: [CommonModule, DecimalPipe, CardComponent, TabNavComponent, FormFieldComponent, StatBoxComponent, BadgeComponent, ButtonComponent, InputComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-4 md:p-6 space-y-6 animate-fade-in">
      <div>
        <h1 class="text-headline font-bold text-fg">Nạp / Rút tiền</h1>
        <p class="text-small text-fg-muted mt-0.5">Quản lý tiền mặt và giao dịch ngân hàng</p>
      </div>

      <!-- Balance stats -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <app-stat-box [data]="{ title: 'Số dư thực tế', value: walletBalance(), prefix: '₫', icon: 'wallet', trend: 'neutral', caption: 'Tổng tiền trong ví' }" />
        <app-stat-box [data]="{ title: 'Khả dụng', value: walletAvailable(), prefix: '₫', icon: 'check-circle', trend: 'up', caption: 'Có thể giao dịch/rút' }" />
        <app-stat-box [data]="{ title: 'Đang phong tỏa', value: walletLocked(), prefix: '₫', icon: 'lock', trend: 'down', caption: 'Chờ khớp lệnh/rút' }" />
      </div>

      <div class="grid grid-cols-1 xl:grid-cols-2 gap-6">

        <!-- Deposit / Withdraw form -->
        <app-card variant="elevated">
          <!-- Mode switch -->
          <div class="flex gap-3 mb-6">
            <button [class]="modeClass('deposit')" (click)="mode.set('deposit')">
              <app-icon name="arrow-down" size="sm" />
              Nạp tiền
            </button>
            <button [class]="modeClass('withdraw')" (click)="mode.set('withdraw')">
              <app-icon name="arrow-up" size="sm" />
              Rút tiền
            </button>
          </div>

          <div class="space-y-5">
            <!-- Amount -->
            <app-form-field label="Số tiền (VNĐ)" fieldId="dep-amount" [required]="true"
              hintMessage="Nạp tối thiểu 100,000 đ · Tối đa 500,000,000 đ/ngày">
              <app-input type="number" inputId="dep-amount" placeholder="10,000,000"
                [(value)]="amount" />
            </app-form-field>

            <!-- Quick amounts -->
            <div class="flex flex-wrap gap-2">
              @for (q of quickAmounts; track q) {
                <button
                  class="px-3 py-1.5 text-xs rounded-lg border border-border bg-surface-2 text-fg-muted hover:border-up/50 hover:text-up transition-all"
                  (click)="amount.set(q)"
                >{{ q | number }} đ</button>
              }
            </div>

            <!-- Bank / Method -->
            @if (mode() === 'deposit') {
              <div>
                <p class="text-small font-medium text-fg mb-3">Phương thức nạp tiền</p>
                <div class="grid grid-cols-2 gap-3">
                  @for (bank of banks; track bank.id) {
                    <button
                      [class]="'flex items-center gap-2.5 p-3 rounded-xl border transition-all text-left ' +
                        (selectedBank() === bank.id ? 'border-up bg-up/5' : 'border-border hover:border-border-hover')"
                      (click)="selectedBank.set(bank.id)"
                    >
                      <div [class]="'w-8 h-8 rounded-lg flex items-center justify-center text-xs font-bold ' + bank.color">
                        {{ bank.abbr }}
                      </div>
                      <div>
                        <p class="text-xs font-semibold text-fg">{{ bank.name }}</p>
                        <p class="text-xs text-fg-muted">{{ bank.fee }}</p>
                      </div>
                    </button>
                  }
                </div>
              </div>
            }

            @if (mode() === 'withdraw') {
              <app-form-field label="Số tài khoản ngân hàng" fieldId="bank-acc" [required]="true">
                <app-input inputId="bank-acc" placeholder="0123 4567 8901 2345" [(value)]="bankAccount" />
              </app-form-field>
              <app-form-field label="Ngân hàng" fieldId="bank-name">
                <app-input inputId="bank-name" placeholder="Vietcombank / BIDV / TPBank..." [(value)]="bankName" />
              </app-form-field>

              <div class="flex items-start gap-2 p-3 rounded-lg bg-reference/10 border border-reference/30">
                <app-icon name="info" size="sm" class="text-reference shrink-0 mt-0.5" />
                <p class="text-xs text-fg-muted">
                  Yêu cầu rút tiền sẽ được xử lý trong <strong>1-2 ngày làm việc</strong>.
                  Phí rút tiền: 0đ.
                </p>
              </div>
            }

            <app-btn
              variant="primary" size="lg" [fullWidth]="true"
              [label]="mode() === 'deposit' ? 'Nạp tiền ngay' : 'Gửi yêu cầu rút tiền'"
              [loading]="isProcessing()"
              (clicked)="process()"
            />
          </div>
        </app-card>

        <!-- Transaction history -->
        <app-card title="Lịch sử giao dịch" variant="default" [hasHeaderAction]="true">
          <ng-container slot="header-action">
            <app-tab-nav [tabs]="historyTabs" [activeId]="historyTab()" variant="pills"
              (activeIdChange)="historyTab.set($event)" />
          </ng-container>

          <div class="space-y-2 mt-4">
            @for (tx of filteredHistory(); track tx.id) {
              <div class="flex items-center gap-3 py-3 border-b border-border/50 last:border-0">
                <!-- Icon -->
                <div [class]="'w-9 h-9 rounded-lg flex items-center justify-center shrink-0 ' +
                  (tx.type === 'deposit' ? 'bg-up/10' : 'bg-down/10')">
                  <app-icon [name]="tx.type === 'deposit' ? 'arrow-down' : 'arrow-up'" size="sm"
                    [class]="tx.type === 'deposit' ? 'text-up' : 'text-down'" />
                </div>
                <!-- Info -->
                <div class="flex-1 min-w-0">
                  <p class="text-small font-medium text-fg">
                    {{ tx.type === 'deposit' ? 'Nạp tiền' : 'Rút tiền' }} · {{ tx.method }}
                  </p>
                  <p class="text-xs text-fg-muted">{{ tx.date }} · {{ tx.ref }}</p>
                </div>
                <!-- Amount + status -->
                <div class="text-right shrink-0">
                  <p [class]="'text-small font-bold font-numeric ' + (tx.type === 'deposit' ? 'text-up' : 'text-down')">
                    {{ tx.type === 'deposit' ? '+' : '-' }}{{ tx.amount | number }} đ
                  </p>
                  <app-badge [variant]="getTxBadgeVariant(tx.status)" size="sm">
                    {{ getTxStatusLabel(tx.status) }}
                  </app-badge>
                </div>
              </div>
            }
          </div>
        </app-card>
      </div>
    </div>
  `,
})
export class DepositComponent implements OnInit {
  private readonly walletService = inject(WalletService);
  private readonly paymentService = inject(PaymentService);
  private readonly transactionService = inject(TransactionService);
  private readonly toast = inject(ToastService);

  readonly mode = signal<TransactionType>('deposit');
  readonly amount = signal<string | number>('');
  readonly bankAccount = signal<string | number>('');
  readonly bankName = signal<string | number>('');
  readonly selectedBank = signal('vcb');
  readonly isProcessing = signal(false);
  readonly historyTab = signal('all');

  readonly walletBalance = signal<number>(0);
  readonly walletAvailable = signal<number>(0);
  readonly walletLocked = signal<number>(0);

  readonly realTransactions = signal<Transaction[]>([]);

  readonly quickAmounts = [500_000, 1_000_000, 5_000_000, 10_000_000, 50_000_000];

  readonly historyTabs: TabItem[] = [
    { id: 'all', label: 'Tất cả' },
    { id: 'deposit', label: 'Nạp' },
    { id: 'withdraw', label: 'Rút' },
  ];

  readonly balanceStats: StatBoxData[] = [
    { title: 'Số dư khả dụng', value: 42_000_000, prefix: '₫', icon: 'wallet', trend: 'neutral', caption: 'Có thể giao dịch' },
    { title: 'Đang rút', value: 5_000_000, prefix: '₫', icon: 'arrow-up', trend: 'down', caption: 'Đang xử lý' },
    { title: 'Nạp tháng này', value: 20_000_000, prefix: '₫', icon: 'arrow-down', trend: 'up', caption: '3 giao dịch' },
  ];

  readonly banks = [
    { id: 'vcb', name: 'Vietcombank', abbr: 'VCB', fee: 'Miễn phí', color: 'bg-green-900 text-green-300' },
    { id: 'tcb', name: 'Techcombank', abbr: 'TCB', fee: 'Miễn phí', color: 'bg-red-900 text-red-300' },
    { id: 'tpb', name: 'TPBank', abbr: 'TPB', fee: 'Miễn phí', color: 'bg-purple-900 text-purple-300' },
    { id: 'bidv', name: 'BIDV', abbr: 'BID', fee: 'Miễn phí', color: 'bg-blue-900 text-blue-300' },
  ];

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.walletService.getWallet().subscribe(res => {
      this.walletBalance.set(res.balance);
      this.walletAvailable.set(res.availableBalance);
      this.walletLocked.set(res.lockedAmount);
    });

    this.transactionService.getMyTransactions().subscribe(txs => {
      const mapped: Transaction[] = txs
        .filter(t => t.transType === 'DEPOSIT' || t.transType === 'WITHDRAW')
        .map(t => ({
          id: t.transId.toString(),
          date: new Date(t.transTime || Date.now()).toLocaleString('vi-VN'),
          type: t.transType === 'DEPOSIT' ? 'deposit' : 'withdraw',
          amount: t.amount,
          status: 'success',
          method: 'Bank Transfer',
          ref: t.refId
        }));

      mapped.reverse();
      this.realTransactions.set(mapped);
    });
  }

  filteredHistory(): Transaction[] {
    const tab = this.historyTab();
    const txs = this.realTransactions();
    return tab === 'all' ? txs : txs.filter(t => t.type === tab);
  }

  modeClass(m: TransactionType): string {
    const base = 'flex-1 flex items-center justify-center gap-2 h-11 rounded-xl font-medium text-small border transition-all ';
    return base + (this.mode() === m
      ? (m === 'deposit' ? 'bg-up text-white border-up shadow-md' : 'bg-down text-white border-down shadow-md')
      : 'border-border text-fg-muted hover:bg-surface-2');
  }

  getTxBadgeVariant(status: Transaction['status']): 'up' | 'reference' | 'down' {
    return { success: 'up' as const, pending: 'reference' as const, failed: 'down' as const }[status];
  }

  getTxStatusLabel(status: Transaction['status']): string {
    return { success: 'Thành công', pending: 'Đang xử lý', failed: 'Thất bại' }[status];
  }

  process(): void {
    const amt = Number(this.amount());
    if (!amt || amt <= 0) return;
    this.isProcessing.set(true);

    if (this.mode() === 'deposit') {
      this.paymentService.createDeposit({
        amount: amt,
        returnUrl: window.location.origin + '/deposit',
        cancelUrl: window.location.origin + '/deposit'
      }).subscribe({
        next: (res) => {
          this.isProcessing.set(false);
          this.amount.set('');
          if (res.checkoutUrl) {
            window.location.href = res.checkoutUrl;
          } else {
            this.toast.success('Nạp tiền thành công!');
            this.loadData();
          }
        },
        error: (err) => {
          this.isProcessing.set(false);
          this.toast.error('Lỗi nạp tiền: ' + (err?.error?.message || err.message || 'Vui lòng thử lại.'));
        }
      });
    } else {
      this.walletService.withdraw(amt).subscribe({
        next: () => {
          this.isProcessing.set(false);
          this.amount.set('');
          this.toast.success('Yêu cầu rút tiền đã được gửi!');
          this.loadData();
        },
        error: (err) => {
          this.isProcessing.set(false);
          this.toast.error('Lỗi rút tiền: ' + (err?.error?.message || err.message || 'Vui lòng thử lại.'));
        }
      });
    }
  }
}
