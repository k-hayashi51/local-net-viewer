export enum FileType {
  None = 0,
  Image = 1,
  Pdf = 2,
  Video = 3,
  Other = 4,
}

export enum ImageShowMode {
  Scroll = 0,
  Page = 1,
}

export interface FileInfoViewModel {
  name: string;
  position: string;
  isDirectory: boolean;
  fileType: FileType;
  childImagePositions: string[];
}

export interface Item {
  id: string;
  name: string;
  path: string;
  type: 'directory' | 'pdf' | 'video';
  thumbnail: string;
  page_count?: number;
  created_at: string;
}

export interface ContentResponse {
  type: 'images' | 'pdf' | 'video';
  images?: string[];
  url?: string;
}
