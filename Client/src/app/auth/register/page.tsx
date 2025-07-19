'use client';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { AuthFormContainer } from '../../../components/features/auth/AuthFormContainer';
import { InputField } from '../../../components/ui/Input/Input';
import { Button } from '../../../components/ui/Button/Button';

export default function RegisterPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    username: '',
    password: '',
    confirmPassword: ''
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [validationErrors, setValidationErrors] = useState({
    username: '',
    password: '',
    confirmPassword: ''
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
    
    if (validationErrors[name as keyof typeof validationErrors]) {
      setValidationErrors(prev => ({
        ...prev,
        [name]: ''
      }));
    }
  };

  const validateForm = () => {
    const errors = {
      username: '',
      password: '',
      confirmPassword: ''
    };
    let isValid = true;

    if (!formData.username.trim()) {
      errors.username = 'Username is required';
      isValid = false;
    } else if (formData.username.length < 3) {
      errors.username = 'Username must be at least 3 characters';
      isValid = false;
    }

    if (!formData.password) {
      errors.password = 'Password is required';
      isValid = false;
    } else if (formData.password.length < 6) {
      errors.password = 'Password must be at least 6 characters';
      isValid = false;
    }

    if (formData.password !== formData.confirmPassword) {
      errors.confirmPassword = 'Passwords do not match';
      isValid = false;
    }

    setValidationErrors(errors);
    return isValid;
  };

const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  setError('');
  
  if (!validateForm()) {
    return;
  }

  setIsLoading(true);
  try {
    const res = await fetch('/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(formData)
    });
    
    if (!res.ok) {
      try {
        const errorData = await res.json();
        setError(errorData.error || 'Registration failed');
      } catch {
        setError(`Registration failed with status ${res.status}`);
      }
      return;
    }

    router.push('/auth/login');
  } catch (err) {
    setError(`Connection error: ${err instanceof Error ? err.message : String(err)}`);
  } finally {
    setIsLoading(false);
  }
};

  const formFooter = (
    <>
      Already have an account?{' '}
      <Link
        href="/auth/login"
        className="text-green-600 hover:text-green-500 font-medium underline underline-offset-4"
      >
        Sign in
      </Link>
    </>
  );

  return (
    <AuthFormContainer
      title="Create Account"
      subtitle="Join our community"
      error={error}
      footer={formFooter}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <InputField
          name="username"
          label="Username"
          type="text"
          placeholder="username"
          value={formData.username}
          onChange={handleChange}
          error={validationErrors.username}
          required
        />

        <InputField
          name="password"
          label="Password"
          type="password"
          placeholder="••••••••"
          value={formData.password}
          onChange={handleChange}
          error={validationErrors.password}
          required
        />

        <InputField
          name="confirmPassword"
          label="Confirm Password"
          type="password"
          placeholder="••••••••"
          value={formData.confirmPassword}
          onChange={handleChange}
          error={validationErrors.confirmPassword}
          required
        />

        <Button
          variant='primary'
          type="submit"
          isLoading={isLoading}
          className="mt-6"
        >
          Create Account
        </Button>
      </form>
    </AuthFormContainer>
  );
}