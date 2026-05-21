type HighlightsProps = {
  highlights: Record<string, string[]>;
};

export function Highlights({ highlights }: HighlightsProps) {
  const entries = Object.entries(highlights).slice(0, 2);
  if (entries.length === 0) {
    return null;
  }

  return (
    <div className="highlights">
      {entries.map(([field, snippets]) => (
        <div key={field}>
          <span>{field}</span>
          <p dangerouslySetInnerHTML={{ __html: snippets[0] }} />
        </div>
      ))}
    </div>
  );
}
