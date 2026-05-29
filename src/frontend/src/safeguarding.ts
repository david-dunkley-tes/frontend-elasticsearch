import type { SafeguardingAnswer, SafeguardingSource } from './types';

/**
 * The sources the answer actually cites (by `[Sxxxxx]` reference in its prose),
 * falling back to every retrieved source when the answer cites none.
 */
export function citedSources(answer: SafeguardingAnswer): SafeguardingSource[] {
  const citedIds = new Set(Array.from(answer.answer.matchAll(/\[(S\d+)\]/g), (match) => match[1]));
  if (citedIds.size === 0) {
    return answer.sources;
  }
  return answer.sources.filter((source) => citedIds.has(source.studentId));
}

/** The distinct student ids cited by an answer (see {@link citedSources}). */
export function citedStudentIds(answer: SafeguardingAnswer): string[] {
  return Array.from(new Set(citedSources(answer).map((source) => source.studentId)));
}
