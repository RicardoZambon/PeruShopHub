/**
 * Reads CSS custom properties at runtime for Chart.js theming.
 * This allows charts to adapt to light/dark theme switches.
 */
export function getCssVar(name: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}

export function getChartColors() {
  return {
    primary: getCssVar('--primary') || '#1A237E',
    primaryAlpha: hexToRgba(getCssVar('--primary') || '#1A237E', 0.1),
    accent: getCssVar('--accent') || '#FF6F00',
    success: getCssVar('--success') || '#2E7D32',
    successAlpha: hexToRgba(getCssVar('--success') || '#2E7D32', 0.1),
    danger: getCssVar('--danger') || '#C62828',
    dangerAlpha: hexToRgba(getCssVar('--danger') || '#C62828', 0.08),
    warning: getCssVar('--warning') || '#F57F17',
    text: getCssVar('--text-primary') || '#212121',
    textSecondary: getCssVar('--text-secondary') || '#757575',
    border: getCssVar('--neutral-200') || '#E0E0E0',
    surface: getCssVar('--surface') || '#FFFFFF',
    gridLine: hexToRgba(getCssVar('--neutral-400') || '#9E9E9E', 0.15),
  };
}

function hexToRgba(hex: string, alpha: number): string {
  if (hex.startsWith('rgba') || hex.startsWith('rgb')) return hex;
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}
