import { resolve } from 'node:path';

const frontendRoot = resolve(__dirname, '../../src/frontend');
const nodeModules = resolve(frontendRoot, 'node_modules');

export default {
  root: resolve(__dirname, '../..'),
  resolve: {
    alias: {
      '@testing-library/jest-dom/vitest': resolve(nodeModules, '@testing-library/jest-dom/vitest.js'),
      '@testing-library/react': resolve(nodeModules, '@testing-library/react/dist/index.js'),
      '@testing-library/user-event': resolve(nodeModules, '@testing-library/user-event/dist/esm/index.js'),
      'lucide-react': resolve(nodeModules, 'lucide-react/dist/esm/lucide-react.js'),
      react: resolve(nodeModules, 'react'),
      'react-dom': resolve(nodeModules, 'react-dom'),
      'react/jsx-dev-runtime': resolve(nodeModules, 'react/jsx-dev-runtime.js'),
      'react/jsx-runtime': resolve(nodeModules, 'react/jsx-runtime.js'),
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    include: ['tests/frontend/**/*.test.tsx'],
  },
};
