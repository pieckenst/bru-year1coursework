import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { formService } from '../services/api';
import type { FormDefinition } from '../types/forms';
import 'survey-core/survey-core.css';

const FormsListPage = () => {
  const [forms, setForms] = useState<FormDefinition[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const loadForms = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const data = await formService.getAllForms();
        setForms(data);
      } catch (err) {
        console.error('Error loading forms:', err);
        setError('Failed to load forms. Please try again later.');
      } finally {
        setIsLoading(false);
      }
    };

    loadForms();
  }, []);

  const handleCreateNew = () => {
    navigate('/forms/new');
  };

  const handleEdit = (id: string) => {
    navigate(`/forms/edit/${id}`);
  };

  const handleView = (id: string) => {
    navigate(`/forms/view/${id}`);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  if (isLoading) {
    return (
      <div className="container mt-4">
        <div className="d-flex justify-content-center">
          <div className="spinner-border" role="status">
            <span className="visually-hidden">Loading...</span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container mt-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1>Forms</h1>
        <button 
          className="btn btn-primary"
          onClick={handleCreateNew}
        >
          Create New Form
        </button>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {forms.length === 0 ? (
        <div className="card">
          <div className="card-body text-center p-5">
            <h3>No forms found</h3>
            <p className="text-muted">Get started by creating a new form</p>
            <button 
              className="btn btn-primary mt-3"
              onClick={handleCreateNew}
            >
              Create Your First Form
            </button>
          </div>
        </div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover">
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Created</th>
                <th>Last Updated</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {forms.map((form) => (
                <tr key={form.id}>
                  <td>{form.name}</td>
                  <td className="text-muted">
                    {form.description || 'No description'}
                  </td>
                  <td>{formatDate(form.createdAt)}</td>
                  <td>
                    {form.updatedAt 
                      ? formatDate(form.updatedAt) 
                      : 'Never'}
                  </td>
                  <td>
                    <span className={`badge ${form.isActive ? 'bg-success' : 'bg-secondary'}`}>
                      {form.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    <div className="btn-group" role="group">
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-primary"
                        onClick={() => handleView(form.id)}
                        title="View Form"
                      >
                        <i className="bi bi-eye"></i>
                      </button>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary"
                        onClick={() => handleEdit(form.id)}
                        title="Edit Form"
                      >
                        <i className="bi bi-pencil"></i>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default FormsListPage;
