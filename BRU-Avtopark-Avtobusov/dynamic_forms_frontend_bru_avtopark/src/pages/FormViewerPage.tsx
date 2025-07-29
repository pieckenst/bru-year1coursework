import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { FormViewer } from '../components/forms/FormViewer';
import { formService } from '../services/api';
import type { FormDefinition } from '../types/forms';
import 'survey-core/survey-core.css';

const FormViewerPage = () => {
  const { id } = useParams<{ id: string }>();
  const [form, setForm] = useState<FormDefinition | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();

  // Load form data
  useEffect(() => {
    const loadForm = async () => {
      try {
        setIsLoading(true);
        setError(null);
        
        if (!id) throw new Error('Form ID is required');
        const data = await formService.getFormById(id);
        setForm(data);
      } catch (err) {
        console.error('Error loading form:', err);
        setError('Failed to load form. It may have been deleted or you may not have permission to view it.');
      } finally {
        setIsLoading(false);
      }
    };

    loadForm();
  }, [id]);

  const handleSubmit = async (formData: any) => {
    try {
      setIsSubmitting(true);
      setError(null);
      
      // Submit the form data
      await formService.submitForm(id!, formData);
      
      // Show success message
      alert('Form submitted successfully!');
      
      // Optionally redirect after successful submission
      // navigate('/forms/thank-you');
    } catch (err) {
      console.error('Error submitting form:', err);
      setError('Failed to submit form. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleError = (errorMessage: string) => {
    setError(errorMessage);
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

  if (!form) {
    return (
      <div className="container mt-4">
        <div className="alert alert-warning" role="alert">
          Form not found.
        </div>
      </div>
    );
  }

  return (
    <div className="form-viewer-page">
      <div className="container py-4">
        <div className="row justify-content-center">
          <div className="col-lg-8">
            <div className="card border-0 shadow-sm mb-4">
              <div className="card-header bg-white border-0">
                <h1 className="h3 mb-0">{form.name}</h1>
                {form.description && (
                  <p className="text-muted mb-0">{form.description}</p>
                )}
              </div>
              <div className="card-body">
                {error && (
                  <div className="alert alert-danger" role="alert">
                    {error}
                  </div>
                )}
                
                <FormViewer 
                  formId={form.id}
                  onSubmit={handleSubmit}
                  onError={handleError}
                />
                
                {isSubmitting && (
                  <div className="text-center mt-3">
                    <div className="spinner-border text-primary" role="status">
                      <span className="visually-hidden">Submitting...</span>
                    </div>
                    <p className="mt-2">Submitting form...</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <style>{`
        /* Ensure the form viewer takes full width */
        .form-viewer-page {
          min-height: 100vh;
          background-color: #f8f9fa;
        }
        
        /* Style the form container */
        .sv_container {
          background-color: #fff;
          border-radius: 0.25rem;
          padding: 1.5rem;
        }
        
        /* Style form elements */
        .sv_q_title {
          font-weight: 500;
          margin-bottom: 0.5rem;
        }
        
        .sv_q_description {
          color: #6c757d;
          font-size: 0.875rem;
          margin-bottom: 1rem;
        }
        
        /* Style form inputs */
        .form-control:focus {
          border-color: #80bdff;
          box-shadow: 0 0 0 0.2rem rgba(0, 123, 255, 0.25);
        }
        
        /* Style buttons */
        .sv_complete_btn, .sv_next_btn, .sv_prev_btn {
          padding: 0.5rem 1.5rem;
          border-radius: 0.25rem;
          font-weight: 500;
          transition: all 0.2s;
        }
        
        .sv_complete_btn, .sv_next_btn {
          background-color: #0d6efd;
          border: 1px solid #0d6efd;
          color: white;
        }
        
        .sv_complete_btn:hover, .sv_next_btn:hover {
          background-color: #0b5ed7;
          border-color: #0a58ca;
        }
        
        .sv_prev_btn {
          background-color: #6c757d;
          border: 1px solid #6c757d;
          color: white;
          margin-right: 0.5rem;
        }
        
        .sv_prev_btn:hover {
          background-color: #5c636a;
          border-color: #565e64;
        }
      `}</style>
    </div>
  );
};

export default FormViewerPage;
