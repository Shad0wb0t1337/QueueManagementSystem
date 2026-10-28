export function LoadingSpinner(): JSX.Element {
  return (
    <div className="flex items-center justify-center p-6">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-zinc-300 border-t-amber-400" />
    </div>
  );
}
