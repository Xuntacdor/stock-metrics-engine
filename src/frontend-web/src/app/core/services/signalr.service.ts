import { Injectable, signal, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
    private hub!: signalR.HubConnection;
    readonly connectionState = signal<'connected' | 'disconnected' | 'connecting'>('disconnected');

    async connect(): Promise<void> {
        if (this.hub?.state === signalR.HubConnectionState.Connected) return;

        const hubUrl = environment.apiUrl.replace('/api', '/hubs/market');

        this.hub = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => localStorage.getItem('auth_token') ?? '',
            })
            .withAutomaticReconnect()
            .build();

        this.hub.onreconnecting(() => this.connectionState.set('connecting'));
        this.hub.onreconnected(() => this.connectionState.set('connected'));
        this.hub.onclose(() => this.connectionState.set('disconnected'));

        try {
            await this.hub.start();
            this.connectionState.set('connected');
            console.log('SignalR Hub Connected');
        } catch (err) {
            console.error('Error while starting connection: ', err);
            this.connectionState.set('disconnected');
        }
    }

    on<T>(event: string, callback: (data: T) => void): void {
        if (!this.hub) return;
        this.hub.on(event, callback);
    }

    off(event: string): void {
        if (!this.hub) return;
        this.hub.off(event);
    }

    ngOnDestroy(): void {
        if (this.hub) {
            this.hub.stop();
        }
    }
}
