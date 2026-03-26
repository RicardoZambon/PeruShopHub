import {
  Component,
  input,
  output,
  signal,
  computed,
  model,
  inject,
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
  Loader2,
} from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { FileUploadService } from '../../services/file-upload.service';
import { ConfirmDialogService } from '../../shared/components';

export interface GalleryImage {
  id: string;
  url: string;
  fileName: string;
  order: number;
}

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
  readonly loaderIcon = Loader2;

  private fileUploadService = inject(FileUploadService);
  private confirmDialog = inject(ConfirmDialogService);

  readonly productId = input<string>('');
  readonly images = model<GalleryImage[]>([]);
  readonly videoUrl = model<string | null>(null);

  readonly imageAdd = output<void>();
  readonly imageRemove = output<number>();

  readonly maxImages = 9;
  readonly uploading = signal(false);

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

  triggerFileInput(): void {
    if (this.images().length >= this.maxImages) return;
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/jpeg,image/png,image/webp';
    input.onchange = (event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (file) this.onFileSelected(file);
    };
    input.click();
  }

  async onFileSelected(file: File): Promise<void> {
    if (this.images().length >= this.maxImages) return;
    if (file.size > 10 * 1024 * 1024) return; // 10MB limit

    const productId = this.productId();
    if (!productId) return;

    this.uploading.set(true);
    try {
      const response = await firstValueFrom(
        this.fileUploadService.upload(file, 'product', productId, this.images().length),
      );
      const newImage: GalleryImage = {
        id: response.id,
        url: response.url,
        fileName: response.fileName,
        order: this.images().length,
      };
      this.images.set([...this.images(), newImage]);
      this.imageAdd.emit();
    } catch {
      // Upload failed — could emit an error event
    } finally {
      this.uploading.set(false);
    }
  }

  async removeImage(index: number): Promise<void> {
    const image = this.images()[index];
    if (!image) return;

    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover imagem',
      message: 'Tem certeza que deseja remover esta imagem?',
      confirmLabel: 'Remover',
      variant: 'danger',
    });

    if (!confirmed) return;

    try {
      await firstValueFrom(this.fileUploadService.delete(image.id));
      this.confirmDialog.done();
    } catch {
      this.confirmDialog.done();
      return;
    }

    const updated = this.images()
      .filter((_, i) => i !== index)
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
