'use client';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { AuthFormContainer } from '../../../components/features/auth/AuthFormContainer';
import { InputField } from '../../../components/ui/Input/Input';
import { Button } from '../../../components/ui/Button/Button';

export default function LoginPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    username: '',
    password: ''
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [validationErrors, setValidationErrors] = useState({
    username: '',
    password: '',
  });

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const res = await fetch('/api/auth/verify');
        if (res.ok) {
          router.replace('/');
        }
      } catch (error) {
        console.error('Auth check failed:', error);
      } finally {
        setIsLoading(false);
      }
    };
    checkAuth();
  }, [router]);

  const validateForm = () => {
    const errors = {
      username: '',
      password: '',
    };
    let isValid = true;

    if (!formData.username.trim()) {
      errors.username = 'Username is required';
      isValid = false;
    }

    if (!formData.password) {
      errors.password = 'Password is required';
      isValid = false;
    }

    setValidationErrors(errors);
    return isValid;
  };

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!validateForm()) {
      return;
    }

    setIsLoading(true);
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          username: formData.username, 
          password: formData.password 
        }),
      });

      if (res.ok) {
        router.replace('/');
      } else {
        const { message } = await res.json();
        setError(message || 'Login failed');
      }
    } catch (err) {
      setError(`Connection error: ${err}`);
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-900">
        <div className="text-green-600 animate-pulse">Checking your session...</div>
      </div>
    );
  }

  const formFooter = (
    <>
      Don&apos;t have an account?{' '}
      <Link
        href="/auth/register"
        className="text-green-600 hover:text-green-500 font-medium underline underline-offset-4"
      >
        Register here
      </Link>
    </>
  );

  return (
    <AuthFormContainer
      title="Welcome Back"
      subtitle="Sign in to your account"
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

        <Button
          variant='primary'
          type="submit"
          isLoading={isLoading}
          className="mt-6"
        >
          Sign In
        </Button>
      </form>
    </AuthFormContainer>
  );
}