export function formatCategoryLabel(category: string): string {
  return category.replace(/([a-z])([A-Z])/g, '$1 $2');
}
