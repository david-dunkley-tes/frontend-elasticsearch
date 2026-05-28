import { RefreshCw } from 'lucide-react';
import type { SafeguardingAnswer, SearchRequest, SearchResponse } from '../types';

type DebugPanelProps = {
  error: string | null;
  loading: boolean;
  request: SearchRequest;
  response: SearchResponse | null;
  safeguardingAnswer: SafeguardingAnswer | null;
};

export function DebugPanel({ error, loading, request, response, safeguardingAnswer }: DebugPanelProps) {
  return (
    <section className="debug-panel">
      <div className="debug-header">
        <h2>Debug Mode</h2>
        <RefreshCw size={16} className={loading ? 'spin' : ''} />
      </div>
      {error && <pre>{error}</pre>}
      <h3>Search request / response</h3>
      <pre>{JSON.stringify({ request, response }, null, 2)}</pre>
      {safeguardingAnswer?.debug && (
        <>
          <h3>LLM call</h3>
          <p className="debug-note">
            Exactly what was sent to the external LLM. Nothing outside this payload leaves the server.
          </p>
          <dl className="debug-llm-meta">
            <dt>Embedding model</dt>
            <dd>{safeguardingAnswer.debug.embeddingModel}</dd>
            <dt>Completion model</dt>
            <dd>{safeguardingAnswer.debug.completionModel}</dd>
            <dt>Retrieved records</dt>
            <dd>{safeguardingAnswer.debug.retrievedCount}</dd>
          </dl>
          <h4>System prompt</h4>
          <pre>{safeguardingAnswer.debug.systemPrompt}</pre>
          <h4>User prompt</h4>
          <pre>{safeguardingAnswer.debug.userPrompt}</pre>
          <h4>Raw completion</h4>
          <pre>{safeguardingAnswer.debug.rawCompletion}</pre>
          <h4>kNN query (Elasticsearch)</h4>
          <pre>{JSON.stringify(safeguardingAnswer.debug.knnQuery, null, 2)}</pre>
        </>
      )}
    </section>
  );
}
