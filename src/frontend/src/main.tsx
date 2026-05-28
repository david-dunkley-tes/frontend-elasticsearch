import { createRoot } from 'react-dom/client';
import { App } from './App';
import { ActiveUserProvider } from './auth/ActiveUserContext';
import './styles.css';

createRoot(document.getElementById('root')!).render(
  <ActiveUserProvider>
    <App />
  </ActiveUserProvider>,
);
