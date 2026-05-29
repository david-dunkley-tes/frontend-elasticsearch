export type Student = {
  id: string;
  foreName: string;
  surname: string;
  fullName: string;
  yearGroup: string;
};

export type School = {
  id: string;
  name: string;
  address: string;
};

export type Trust = {
  id: string;
  name: string;
} | null;

export type SafeguardingLog = {
  category: string;
  date: string;
  narrative: string;
};

export type ClassGroup = {
  name: string;
  teacher: string;
};

export type SearchResult = {
  id: string;
  student: Student;
  school: School;
  trust: Trust;
  classGroup?: ClassGroup | null;
  safeguardingLog?: SafeguardingLog | null;
  highlights: Record<string, string[]>;
  score?: number | null;
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

export type SearchDebug = {
  elasticsearchQuery: string;
  selectedFilters: Record<string, string[]>;
};

export type SearchResponse = {
  total: number;
  tookMs: number;
  backendTookMs: number;
  results: SearchResult[];
  facets: Record<string, Facet>;
  debug?: SearchDebug | null;
};

export type Filters = Record<string, string[]>;

export type SearchRequest = {
  query: string;
  filters: Filters;
  studentIds?: string[];
  sort: string;
  page: number;
  pageSize: number;
  debugMode: boolean;
};

export type SavedSearch = {
  id: string;
  name: string;
  query: string;
  filters: Filters;
  sort: string;
  pageSize: number;
  createdAt: string;
};

export type SaveSearchRequest = {
  name: string;
  query: string;
  filters: Filters;
  sort: string;
  pageSize: number;
};

export type AuthorizationScope = {
  type: string;
  schoolId?: string;
  trustId?: string;
  schoolGroupId?: string;
  schoolIds?: string[];
};

export type SafeguardingSource = {
  studentId: string;
  fullName: string;
  yearGroup: string;
  schoolId: string;
  schoolName: string;
  trustName?: string | null;
  category: string;
  date: string;
  narrative: string;
  score?: number | null;
};

export type RagDebug = {
  embeddingModel: string;
  completionModel: string;
  retrievedCount: number;
  knnQuery?: string | null;
  systemPrompt: string;
  userPrompt: string;
  rawCompletion: string;
};

export type SafeguardingAnswer = {
  answer: string;
  sources: SafeguardingSource[];
  debug?: RagDebug | null;
};

export type SafeguardingAvailability = {
  available: boolean;
  reason?: string | null;
};

export type VersionInfo = {
  service: string;
  version: string;
  commit: string;
  buildTime: string;
  environment: string;
};
