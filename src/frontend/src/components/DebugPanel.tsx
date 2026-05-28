import { RefreshCw } from 'lucide-react';
import type { RagAnswer, SearchRequest, SearchResponse } from '../types';

type DebugPanelProps = {
  error: string | null;
  loading: boolean;
  request: SearchRequest;
  response: SearchResponse | null;
  ragAnswer: RagAnswer | null;
};

export function DebugPanel({ error, loading, request, response, ragAnswer }: DebugPanelProps) {
  return (
    <section className="debug-panel">
      <div className="debug-header">
        <h2>Debug Mode</h2>
        <RefreshCw size={16} className={loading ? 'spin' : ''} />
      </div>
      {error && <pre>{error}</pre>}
      <h3>Search request / response</h3>
      <pre>{JSON.stringify({ request, response }, null, 2)}</pre>
      {ragAnswer?.debug && (
        <>
          <h3>LLM call</h3>
          <p className="debug-note">
            Exactly what was sent to the external LLM. Nothing outside this payload leaves the server.
          </p>
          <dl className="debug-llm-meta">
            <dt>Embedding model</dt>
            <dd>{ragAnswer.debug.embeddingModel}</dd>
            <dt>Completion model</dt>
            <dd>{ragAnswer.debug.completionModel}</dd>
            <dt>Retrieved records</dt>
            <dd>{ragAnswer.debug.retrievedCount}</dd>
          </dl>
          <h4>System prompt</h4>
          <pre>{ragAnswer.debug.systemPrompt}</pre>
          <h4>User prompt</h4>
          <pre>{ragAnswer.debug.userPrompt}</pre>
          <h4>Raw completion</h4>
          <pre>{ragAnswer.debug.rawCompletion}</pre>
          <h4>kNN query (Elasticsearch)</h4>
          <pre>{JSON.stringify(ragAnswer.debug.knnQuery, null, 2)}</pre>
        </>
      )}
    </section>
  );
}
