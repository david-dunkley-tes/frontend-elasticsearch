export type Student = {
  id: string;
  foreName: string;
  surname: string;
  fullName: string;
  yearGroup: string;
};

export type School = {
  name: string;
  address: string;
};

export type Trust = {
  name: string;
} | null;

export type SearchResult = {
  id: string;
  student: Student;
  school: School;
  trust: Trust;
  highlights: Record<string, string[]>;
  score?: number;
};

export type FacetOption = {
  value: string;
  label: string;
  count: number;
  selected: boolean;
};

export type Facet = {
  label: string;
  type: string;
  selected: string[];
  options: FacetOption[];
};

export type SearchResponse = {
  total: number;
  tookMs: number;
  backendTookMs: number;
  results: SearchResult[];
  facets: Record<string, Facet>;
  debug?: unknown;
};

export type Filters = Record<string, string[]>;

export type SearchRequest = {
  query: string;
  filters: Filters;
  sort: string;
  page: number;
  pageSize: number;
  debugMode: boolean;
};
