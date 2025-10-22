import { Injectable, signal } from '@angular/core';

export interface UserInfo {
  name: string;
  email: string;
  phone: string;
}

@Injectable({
  providedIn: 'root',
})
export class UserService {
  // Default user data as requested
  private userInfo = signal<UserInfo>({
    name: 'Adi Dimi',
    email: 'adidimi@example.com',
    phone: '+972529213125',
  });

  // Read-only access to user info
  readonly user = this.userInfo.asReadonly();

  // Methods to update user info if needed later
  updateUser(info: Partial<UserInfo>) {
    this.userInfo.update((current) => ({ ...current, ...info }));
  }

  setUser(info: UserInfo) {
    this.userInfo.set(info);
  }
}
