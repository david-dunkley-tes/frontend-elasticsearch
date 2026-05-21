import type { SearchResult } from '../types';
import { Highlights } from './Highlights';

type StudentDetailProps = {
  result: SearchResult;
};

export function StudentDetail({ result }: StudentDetailProps) {
  return (
    <div className="detail-content">
      <div>
        <span className="eyebrow">Student</span>
        <h3>{result.student.fullName}</h3>
        <dl>
          <dt>ID</dt>
          <dd>{result.student.id}</dd>
          <dt>Year group</dt>
          <dd>{result.student.yearGroup}</dd>
          <dt>Score</dt>
          <dd>{result.score?.toFixed(3) ?? 'N/A'}</dd>
        </dl>
      </div>
      <div>
        <span className="eyebrow">School</span>
        <h3>{result.school.name}</h3>
        <p>{result.school.address}</p>
      </div>
      <div>
        <span className="eyebrow">Trust</span>
        <h3>{result.trust?.name ?? 'No trust'}</h3>
      </div>
      <div>
        <span className="eyebrow">Matched fields</span>
        <Highlights highlights={result.highlights} />
      </div>
    </div>
  );
}
