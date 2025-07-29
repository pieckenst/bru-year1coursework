import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { FormBuilder } from '../components/forms/FormBuilder';
import { formService } from '../services/api';
import type { FormDefinition } from '../types/forms';
import 'survey-core/survey-core.css';

const FormEditorPage = () => {
  const { id } = useParams<{ id?: string }>();
  const [isNew] = useState(!id);
  const [form, setForm] = useState<FormDefinition | null>(null);
  const [isLoading, setIsLoading] = useState(!isNew);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  // Load form data if editing
  useEffect(() => {
    if (isNew) return;

    const loadForm = async () => {
      try {
        setIsLoading(true);
        setError(null);
        
        if (!id) throw new Error('Form ID is required');
        const data = await formService.getFormById(id);
        setForm(data);
      } catch (err) {
        console.error('Error loading form:', err);
        setError('Failed to load form. Please try again.');
      } finally {
        setIsLoading(false);
      }
    };

    loadForm();
  }, [id, isNew]);

  const handleSave = (savedForm: FormDefinition) => {
    // If this was a new form, redirect to edit page with the new ID
    if (isNew) {
      navigate(`/forms/edit/${savedForm.id}`, { replace: true });
    }
    setForm(savedForm);
    
    // Show success message
    // You might want to use a toast notification here instead
    alert('Form saved successfully!');
  };

  const handleCancel = () => {
    navigate('/forms');
  };

  if (!isNew && isLoading) {
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

  if (error) {
    return (
      <div className="container mt-4">
        <div className="alert alert-danger" role="alert">
          {error}
          <div className="mt-3">
            <button 
              className="btn btn-secondary"
              onClick={() => window.location.reload()}
            >
              Retry
            </button>
            <button 
              className="btn btn-outline-secondary ms-2"
              onClick={() => navigate('/forms')}
            >
              Back to Forms
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="form-editor-page">
      <div className="container-fluid p-0">
        <div className="row g-0">
          <div className="col-12">
            <FormBuilder 
              formId={id}
              onSave={handleSave}
              onCancel={handleCancel}
            />
          </div>
        </div>
      </div>
      
      <style>{`
        /* Ensure the form builder takes full height */
        html, body, #root, .form-editor-page {
          height: 100%;
          margin: 0;
        }
        
        /* Override some default styles that might interfere with the form builder */
        body {
          overflow-x: hidden;
        }
        
        /* Make sure the form builder container takes full height */
        .form-builder-container {
          height: 100%;
          display: flex;
          flex-direction: column;
        }
        
        /* Style the form builder header */
        .form-builder-header {
          background-color: #f8f9fa;
          padding: 1rem;
          border-bottom: 1px solid #dee2e6;
          z-index: 1000;
        }
        
        /* Style the form builder content area */
        .form-builder {
          flex: 1;
          min-height: calc(100vh - 60px);
          position: relative;
        }
        
        /* Style the survey creator tabs */
        .svc-tabbed-menu {
          background-color: #f8f9fa !important;
          border-bottom: 1px solid #dee2e6 !important;
        }
        
        /* Style the property grid */
        .spg-row {
          margin-bottom: 0.5rem;
        }
        
        /* Style the toolbox */
        .svc-toolbox {
          background-color: #f8f9fa !important;
          border-right: 1px solid #dee2e6 !important;
        }
        
        /* Style the preview area */
        .svc-creator__content-frame {
          background-color: #fff !important;
        }
      `}</style>
    </div>
  );
};

export default FormEditorPage;
