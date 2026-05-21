import { RefreshCw } from 'lucide-react';
import type { SearchRequest, SearchResponse } from '../types';

type DebugPanelProps = {
  error: string | null;
  loading: boolean;
  request: SearchRequest;
  response: SearchResponse | null;
};

export function DebugPanel({ error, loading, request, response }: DebugPanelProps) {
  return (
    <section className="debug-panel">
      <div className="debug-header">
        <h2>Debug Mode</h2>
        <RefreshCw size={16} className={loading ? 'spin' : ''} />
      </div>
      {error && <pre>{error}</pre>}
      <pre>{JSON.stringify({ request, response }, null, 2)}</pre>
    </section>
  );
}
