import { Component, Input, output } from '@angular/core';
import { CommentsComponent } from '../comments/comments';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-post-card',
  imports: [CommentsComponent],
  templateUrl: './post-card.html',
  styleUrl: './post-card.scss',
})
export class PostCard {
  @Input() title = '';
  @Input() category = '';
  @Input() location = '';
  @Input() author?: string;
  @Input() body = '';
  @Input() image?: string;
  @Input() dir: 'ltr' | 'rtl' = 'ltr';
  @Input() adId: string | null | undefined;
  @Input() contactName?: string; // To check ownership

  onEdit = output<string>(); // Emit the ad ID for editing
  onDelete = output<string>(); // Emit the ad ID for deletion

  constructor(private user: UserService) {}

  get canEdit(): boolean {
    // User can edit if they are the author or if their name matches the contact name
    const currentUserName = this.user.user().name;
    const canEdit = this.author === currentUserName || this.contactName === currentUserName;
    console.log('PostCard canEdit check:', {
      currentUserName,
      author: this.author,
      contactName: this.contactName,
      adId: this.adId,
      canEdit,
    });
    return canEdit;
  }

  editPost() {
    console.log('PostCard editPost clicked:', {
      adId: this.adId,
      canEdit: this.canEdit,
      author: this.author,
      contactName: this.contactName,
    });
    if (this.adId && this.canEdit) {
      console.log('Emitting edit event for adId:', this.adId);
      this.onEdit.emit(this.adId);
    } else {
      console.log('Edit not allowed or no adId');
    }
  }

  deletePost() {
    console.log('PostCard deletePost clicked:', {
      adId: this.adId,
      canEdit: this.canEdit,
      author: this.author,
      contactName: this.contactName,
    });
    if (
      this.adId &&
      this.canEdit &&
      confirm('Are you sure you want to delete this post? This action cannot be undone.')
    ) {
      console.log('Emitting delete event for adId:', this.adId);
      this.onDelete.emit(this.adId);
    } else {
      console.log('Delete not allowed, no adId, or user cancelled');
    }
  }
}
