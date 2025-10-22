import { Component, output, signal, input, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdsService } from '../../services/ads.service';
import { Ad } from '../../models/generated/models/ad';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-new-post-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './new-post-form.html',
  styleUrl: './new-post-form.scss',
})
export class NewPostFormComponent {
  onAdCreated = output<any>();
  onAdUpdated = output<any>();
  onCancel = output<void>();

  // Input for edit mode
  editAd = input<Ad | null>(null);

  posting = signal(false);
  error = signal<string | null>(null);

  // Form fields
  title = signal('');
  body = signal('');
  category = signal('');
  price = signal<number | null>(null);
  tags = signal('');
  address = signal('');

  constructor(private ads: AdsService, private user: UserService) {
    // Effect to populate form when editAd changes
    effect(() => {
      const ad = this.editAd();
      if (ad) {
        this.populateForm(ad);
      } else {
        this.resetForm();
      }
    });
  }

  get isEditMode(): boolean {
    return this.editAd() !== null;
  }

  get formTitle(): string {
    return this.isEditMode ? 'Edit Post' : 'Create New Post';
  }

  get submitButtonText(): string {
    if (this.posting()) {
      return this.isEditMode ? 'Updating...' : 'Creating...';
    }
    return this.isEditMode ? 'Update Post' : 'Create Post';
  }

  populateForm(ad: Ad) {
    this.title.set(ad.title || '');
    this.body.set(ad.body || '');
    this.category.set(ad.category || '');
    this.price.set(ad.price || null);
    this.address.set(ad.location?.address || '');
    this.tags.set(ad.tags?.join(', ') || '');
  }
  submitForm() {
    if (!this.title().trim()) {
      this.error.set('Title is required');
      return;
    }

    this.posting.set(true);
    this.error.set(null);

    const payload = {
      title: this.title().trim(),
      body: this.body().trim(),
      category: this.category().trim() || undefined,
      price: this.price() || undefined,
      contact: {
        name: this.user.user().name,
        email: this.user.user().email,
        phone: this.user.user().phone,
      },
      location: this.address().trim()
        ? {
            address: this.address().trim(),
            lat: undefined, // Generated interface allows optional lat/lng
            lng: undefined,
          }
        : undefined,
      tags: this.tags().trim()
        ? this.tags()
            .split(',')
            .map((tag) => tag.trim())
            .filter((tag) => tag)
        : undefined,
    };

    if (this.isEditMode) {
      const adId = this.editAd()!.id!;
      this.ads.updateAd(adId, payload).subscribe({
        next: (updatedAd) => {
          console.log('Ad updated:', updatedAd);
          this.onAdUpdated.emit(updatedAd);
          this.posting.set(false);
        },
        error: (err) => {
          console.error('Error updating ad:', err);
          this.error.set(err?.message ?? 'Failed to update ad');
          this.posting.set(false);
        },
      });
    } else {
      this.ads.createAd(payload).subscribe({
        next: (newAd) => {
          console.log('New ad created:', newAd);
          this.onAdCreated.emit(newAd);
          this.resetForm();
          this.posting.set(false);
        },
        error: (err) => {
          console.error('Error creating ad:', err);
          this.error.set(err?.message ?? 'Failed to create ad');
          this.posting.set(false);
        },
      });
    }
  }

  resetForm() {
    this.title.set('');
    this.body.set('');
    this.category.set('');
    this.price.set(null);
    this.tags.set('');
    this.address.set('');
  }

  cancel() {
    if (!this.isEditMode) {
      this.resetForm();
    }
    this.onCancel.emit();
  }
}
