import React from 'react';
import { SearchX, Sparkles } from 'lucide-react';
import { askSafeguarding } from '../api/studentSearchApi';
import { formatCategoryLabel } from '../format';
import { citedSources } from '../safeguarding';
import type { SafeguardingAnswer, SafeguardingSource } from '../types';

type AskPanelProps = {
  enabled: boolean;
  disabledReason: string | null;
  debugMode: boolean;
  onAnswerChange: (answer: SafeguardingAnswer | null) => void;
  onSourceClick: (source: SafeguardingSource) => void;
};

export function AskPanel({ enabled, disabledReason, debugMode, onAnswerChange, onSourceClick }: AskPanelProps) {
  const [question, setQuestion] = React.useState('');
  const [pending, setPending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [answer, setAnswer] = React.useState<SafeguardingAnswer | null>(null);

  React.useEffect(() => {
    onAnswerChange(answer);
  }, [answer, onAnswerChange]);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = question.trim();
    if (!trimmed || pending || !enabled) {
      return;
    }

    setPending(true);
    setError(null);
    try {
      const next = await askSafeguarding(trimmed, debugMode);
      setAnswer(next);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ask failed');
      setAnswer(null);
    } finally {
      setPending(false);
    }
  }

  const sourcesToShow = answer ? citedSources(answer) : [];

  return (
    <details className="ask-panel">
      <summary className="ask-summary">
        <Sparkles size={14} className="ask-icon" aria-hidden />
        <span>Ask the safeguarding records (AI)</span>
        {!enabled && <span className="ask-summary-tag">unavailable</span>}
      </summary>
      <div className="ask-body">
        <form onSubmit={submit} className="ask-form">
          <input
            type="text"
            className="ask-input"
            placeholder={enabled ? 'Ask about safeguarding records…' : 'AI Ask is not configured'}
            value={question}
            onChange={(event) => setQuestion(event.target.value)}
            disabled={!enabled || pending}
            aria-label="Safeguarding question"
          />
          <button type="submit" className="ask-submit" disabled={!enabled || pending || question.trim().length === 0}>
            {pending ? 'Asking…' : 'Ask'}
          </button>
        </form>

        {!enabled && disabledReason && (
          <p className="ask-disabled">AI Ask is unavailable: {disabledReason}.</p>
        )}

        {error && <p className="ask-error">{error}</p>}

        {answer && (
          <div className="ask-result">
            {sourcesToShow.length === 0 && (
              <p className="ask-no-matches">
                <SearchX size={16} aria-hidden />
                No matching safeguarding records found for this question.
              </p>
            )}
            <details className="ask-answer-details">
              <summary>AI summary</summary>
              <div className="ask-answer">{renderInlineMarkdown(answer.answer)}</div>
            </details>
            {sourcesToShow.length > 0 && (
              <div className="ask-sources">
                <h4>Cited sources ({sourcesToShow.length} of {answer.sources.length} retrieved)</h4>
                <ul>
                  {sourcesToShow.map((source) => (
                    <li key={source.studentId}>
                      <div
                        className="ask-source"
                        role="button"
                        tabIndex={0}
                        onClick={() => {
                          // Don't navigate if the click was the end of a text selection — let the user copy.
                          if ((window.getSelection()?.toString() ?? '').length > 0) {
                            return;
                          }
                          onSourceClick(source);
                        }}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter' || event.key === ' ') {
                            event.preventDefault();
                            onSourceClick(source);
                          }
                        }}
                      >
                        <div className="ask-source-header">
                          <span className="ask-source-id">[{source.studentId}]</span>
                          <span className="ask-source-name">{source.fullName}</span>
                          <span className="ask-source-meta">{source.yearGroup} · {source.schoolName}</span>
                          <span className="ask-source-category">{formatCategoryLabel(source.category)}</span>
                        </div>
                        <p className="ask-source-narrative">{source.narrative}</p>
                      </div>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}
      </div>
    </details>
  );
}

function renderInlineMarkdown(text: string): React.ReactNode {
  const lines = text.split(/\r?\n/);
  return lines.map((line, lineIndex) => (
    <React.Fragment key={lineIndex}>
      {renderBoldSegments(line)}
      {lineIndex < lines.length - 1 && <br />}
    </React.Fragment>
  ));
}

function renderBoldSegments(line: string): React.ReactNode {
  const parts = line.split(/(\*\*[^*]+\*\*)/g);
  return parts.map((part, index) => {
    if (part.startsWith('**') && part.endsWith('**') && part.length > 4) {
      return <strong key={index}>{part.slice(2, -2)}</strong>;
    }
    return <React.Fragment key={index}>{part}</React.Fragment>;
  });
}
