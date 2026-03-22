import {
  Component,
  input,
  output,
  signal,
  computed,
  model,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CdkDragDrop,
  CdkDrag,
  CdkDragHandle,
  CdkDragPlaceholder,
  CdkDropList,
  moveItemInArray,
} from '@angular/cdk/drag-drop';
import {
  LucideAngularModule,
  Camera,
  Play,
  X,
  Star,
  GripVertical,
  Video,
} from 'lucide-angular';

export interface GalleryImage {
  id: string;
  color: string;
  order: number;
}

const PLACEHOLDER_COLORS = [
  '#5C6BC0', '#42A5F5', '#66BB6A', '#FFA726', '#EF5350',
  '#AB47BC', '#26C6DA', '#8D6E63', '#78909C',
];

@Component({
  selector: 'app-media-gallery',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CdkDrag,
    CdkDragHandle,
    CdkDragPlaceholder,
    CdkDropList,
    LucideAngularModule,
  ],
  templateUrl: './media-gallery.component.html',
  styleUrl: './media-gallery.component.scss',
})
export class MediaGalleryComponent {
  readonly cameraIcon = Camera;
  readonly playIcon = Play;
  readonly closeIcon = X;
  readonly starIcon = Star;
  readonly gripIcon = GripVertical;
  readonly videoIcon = Video;

  readonly images = model<GalleryImage[]>([]);
  readonly videoUrl = model<string | null>(null);

  readonly imageAdd = output<void>();
  readonly imageRemove = output<number>();

  readonly maxImages = 9;
  private nextColorIndex = 0;

  videoInputValue = signal('');

  imageCount = computed(() => this.images().length);
  emptySlots = computed(() => {
    const count = this.maxImages - this.images().length;
    return count > 0 ? Array(count).fill(0) : [];
  });

  videoThumbnail = computed(() => {
    const url = this.videoUrl();
    if (!url) return null;
    const videoId = this.extractYouTubeId(url);
    if (!videoId) return null;
    return `https://img.youtube.com/vi/${videoId}/mqdefault.jpg`;
  });

  onVideoUrlInput(value: string): void {
    this.videoInputValue.set(value);
    if (value.trim()) {
      const videoId = this.extractYouTubeId(value);
      if (videoId) {
        this.videoUrl.set(value);
      }
    }
  }

  clearVideo(): void {
    this.videoUrl.set(null);
    this.videoInputValue.set('');
  }

  addMockImage(): void {
    if (this.images().length >= this.maxImages) return;

    const color = PLACEHOLDER_COLORS[this.nextColorIndex % PLACEHOLDER_COLORS.length];
    this.nextColorIndex++;

    const newImage: GalleryImage = {
      id: 'img-' + Math.random().toString(36).substring(2, 8),
      color,
      order: this.images().length,
    };

    this.images.set([...this.images(), newImage]);
    this.imageAdd.emit();
  }

  removeImage(index: number): void {
    const updated = this.images().filter((_, i) => i !== index)
      .map((img, i) => ({ ...img, order: i }));
    this.images.set(updated);
    this.imageRemove.emit(index);
  }

  onDrop(event: CdkDragDrop<GalleryImage[]>): void {
    const reordered = [...this.images()];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);
    this.images.set(reordered.map((img, i) => ({ ...img, order: i })));
  }

  private extractYouTubeId(url: string): string | null {
    // Match various YouTube URL formats
    const patterns = [
      /(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})/,
      /^([a-zA-Z0-9_-]{11})$/,
    ];
    for (const pattern of patterns) {
      const match = url.match(pattern);
      if (match) return match[1];
    }
    return null;
  }
}
