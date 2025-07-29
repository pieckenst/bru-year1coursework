import { BrowserRouter as Router, Routes, Route, Navigate, Link } from 'react-router-dom';
import { useState } from 'react';
import './App.css';

// Pages
import FormsListPage from './pages/FormsListPage';
import FormEditorPage from './pages/FormEditorPage';
import FormViewerPage from './pages/FormViewerPage';

// Layout components
const Layout = ({ children }: { children: React.ReactNode }) => {
  return (
    <div className="app-layout">
      <header className="app-header">
        <nav>
          <Link to="/" className="logo">Form Builder</Link>
          <div className="nav-links">
            <Link to="/forms" className="nav-link">My Forms</Link>
            <Link to="/forms/new" className="btn btn-primary">Create New</Link>
          </div>
        </nav>
      </header>
      
      <main className="app-main">
        {children}
      </main>
      
      <footer className="app-footer">
        &copy; {new Date().getFullYear()} BRU Avtopark - Dynamic Forms
      </footer>
    </div>
  );
};

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Navigate to="/forms" replace />} />
        <Route path="/forms" element={
          <Layout>
            <FormsListPage />
          </Layout>
        } />
        <Route path="/forms/new" element={
          <Layout>
            <FormEditorPage />
          </Layout>
        } />
        <Route path="/forms/edit/:id" element={
          <Layout>
            <FormEditorPage />
          </Layout>
        } />
        <Route path="/forms/view/:id" element={
          <Layout>
            <FormViewerPage />
          </Layout>
        } />
        <Route path="*" element={
          <Layout>
            <div className="not-found">
              <h2>404 - Page Not Found</h2>
              <p>The page you are looking for does not exist.</p>
              <Link to="/" className="btn btn-primary">Go to Home</Link>
            </div>
          </Layout>
        } />
      </Routes>
    </Router>
  );
}

export default App;
