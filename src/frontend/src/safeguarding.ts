import type { SafeguardingAnswer, SafeguardingSource } from './types';

/**
 * The sources the answer actually cites (by `[Sxxxxx]` reference in its prose).
 * If the answer cites nobody (e.g. a "no relevant records" reply), this is empty — we must NOT
 * fall back to the full retrieved set, or a no-results answer would still list/▸filter the
 * retrieved-but-irrelevant records.
 */
export function citedSources(answer: SafeguardingAnswer): SafeguardingSource[] {
  const citedIds = new Set(Array.from(answer.answer.matchAll(/\[(S\d+)\]/g), (match) => match[1]));
  return answer.sources.filter((source) => citedIds.has(source.studentId));
}

/** The distinct student ids cited by an answer (see {@link citedSources}). */
export function citedStudentIds(answer: SafeguardingAnswer): string[] {
  return Array.from(new Set(citedSources(answer).map((source) => source.studentId)));
}
