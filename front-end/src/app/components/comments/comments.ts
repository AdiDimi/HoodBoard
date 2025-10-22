import { Component, input, signal, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdsService } from '../../services/ads.service';
import { Comment } from '../../models/generated/models/comment';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-comments',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './comments.html',
  styleUrl: './comments.scss',
})
export class CommentsComponent {
  adId = input<string>('');
  loading = signal(false);
  error = signal<string | null>(null);
  comments = signal<Comment[]>([]);
  newText = signal('');
  posting = signal(false);

  constructor(private ads: AdsService, private user: UserService) {
    effect(() => {
      const id = this.adId();
      if (!id) {
        this.comments.set([]);
        return;
      }
      this.fetch(id);
    });
  }

  private fetch(id: string) {
    this.loading.set(true);
    this.error.set(null);
    this.ads.getComments(id).subscribe({
      next: (data) => {
        this.comments.set(data ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.message ?? 'Failed to load comments');
        this.loading.set(false);
      },
    });
  }

  postComment() {
    const id = this.adId();
    const text = this.newText().trim();
    if (!id || !text) return;

    this.posting.set(true);
    console.log('Posting comment:', { id, authorName: this.user.user().name, text });

    this.ads
      .postComment(id, {
        authorName: this.user.user().name,
        text: text,
      })
      .subscribe({
        next: (newComment) => {
          console.log('Comment posted successfully:', newComment);
          // Add to local list optimistically
          this.comments.update((current) => [...current, newComment]);
          this.newText.set('');
          this.posting.set(false);
        },
        error: (err) => {
          console.error('Error posting comment:', err);
          this.error.set(err?.message ?? 'Failed to post comment');
          this.posting.set(false);
        },
      });
  }
}
