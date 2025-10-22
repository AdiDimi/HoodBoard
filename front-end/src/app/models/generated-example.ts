// This is an example of what will be generated when you run `npm run api:gen`
// The actual generated files will replace the manual interfaces in ads.service.ts

// Example generated models (these will be auto-generated from your Swagger spec):

export interface Ad {
  id?: string;
  title?: string;
  body?: string;
  category?: string;
  price?: number;
  tags?: Array<string>;
  location?: Location;
  contact?: Contact;
  comments?: Array<Comment>;
  photos?: Array<Photo>;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface Location {
  lat: number;
  lng: number;
  address?: string;
}

export interface Contact {
  name?: string;
  email?: string;
  phone?: string;
}

export interface Comment {
  id?: string;
  authorName?: string;
  text?: string;
  createdAt?: string;
}

export interface Photo {
  id?: string;
  url?: string;
  thumbUrl?: string;
  largeUrl?: string;
}

export interface ApiListResponse<T> {
  data?: Array<T>;
  meta?: PaginationMeta;
}

export interface PaginationMeta {
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}

// Example generated service (this will also be auto-generated):
/*
export class DefaultService {
  // Generated methods for all your API endpoints:
  // - searchAds()
  // - createAd()
  // - updateAd() 
  // - deleteAd()
  // - getComments()
  // - createComment()
  // etc.
}
*/
