import { Component, signal, ViewChild } from '@angular/core';
import { Header } from './components/header/header';
import { SearchBar } from './components/search-bar/search-bar';
import { PostBoard } from './components/post-board/post-board';
import { NewPostFormComponent } from './components/new-post-form/new-post-form';
import { AdsService } from './services/ads.service';
import { Ad } from './models/generated/models/ad';

@Component({
  selector: 'app-root',
  imports: [Header, SearchBar, PostBoard, NewPostFormComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  @ViewChild(PostBoard) postBoard!: PostBoard;

  protected readonly title = signal('hood-board');
  query = signal('');
  showCreateForm = signal(false);
  editingAd = signal<Ad | null>(null);

  constructor(private adsService: AdsService) {}

  get showForm(): boolean {
    const shouldShow = this.showCreateForm() || this.editingAd() !== null;
    console.log('App showForm getter:', {
      showCreateForm: this.showCreateForm(),
      editingAd: this.editingAd(),
      shouldShow,
    });
    return shouldShow;
  }

  get currentEditAd(): Ad | null {
    const ad = this.editingAd();
    console.log('App currentEditAd getter:', ad);
    return ad;
  }

  onSearch(query: string) {
    this.query.set(query);
  }

  onCreatePost() {
    this.editingAd.set(null); // Clear any edit mode
    this.showCreateForm.set(true);
  }

  onEditPost(adId: string) {
    console.log('App onEditPost called with adId:', adId);
    // Find the ad to edit
    const ads = this.postBoard.getAds();
    console.log(
      'Available ads:',
      ads.map((ad) => ({ id: ad.id, title: ad.title }))
    );
    const adToEdit = ads.find((ad) => ad.id === adId);
    console.log('Found ad to edit:', adToEdit);
    if (adToEdit) {
      this.showCreateForm.set(false); // Clear create mode
      this.editingAd.set(adToEdit);
      console.log('Edit mode activated for ad:', adToEdit.title);
      console.log('editingAd signal value:', this.editingAd());
      console.log('showForm should be true:', this.showForm);
    } else {
      console.log('Ad not found for editing');
    }
  }

  onAdCreated(newAd: any) {
    this.showCreateForm.set(false);
    this.postBoard.refreshData();
  }

  onAdUpdated(updatedAd: any) {
    this.editingAd.set(null);
    this.postBoard.refreshData();
  }

  onDeletePost(adId: string) {
    console.log('App onDeletePost called with adId:', adId);
    this.adsService.deleteAd(adId).subscribe({
      next: () => {
        console.log('Ad deleted successfully:', adId);
        this.postBoard.refreshData();
      },
      error: (err) => {
        console.error('Error deleting ad:', err);
        alert('Failed to delete post: ' + (err?.message ?? 'Unknown error'));
      },
    });
  }

  onCancelForm() {
    this.showCreateForm.set(false);
    this.editingAd.set(null);
  }
}
