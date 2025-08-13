import { useEffect, useState, useRef } from 'react';
import { Avatar } from '@/components/ui/Avatar/Avatar';
import { Button } from '@/components/ui/Button/Button';
import { InputField } from '@/components/ui/Input/Input';
import { TextArea } from '@/components/ui/TextArea/TextArea';
import { EditIcon } from '@/components/icons/EditIcon';
import { SaveIcon } from '@/components/icons/SaveIcon';
import { CancelIcon } from '@/components/icons/CancelIcon';
import { UserProfileDto } from '@/types';
import { LeftArrowIcon } from '@/components/icons/LeftArrowIcon';

interface UserSettingsProps {
  onClose: () => void;
}

export const UserSettings = ({ onClose }: UserSettingsProps) => {
  const [profile, setProfile] = useState<UserProfileDto | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<Partial<UserProfileDto>>({});
  const [bioLength, setBioLength] = useState(0);
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const response = await fetch('/api/users/profile', {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        });
        
        if (response.ok) {
          const data = await response.json();
          setProfile(data);
          setFormData(data);
          setBioLength(data.bio?.length || 0);
        } else {
          setError('Failed to load profile');
        }
      } catch (err) {
        setError(`Network error occurred: ${err}`);
      } finally {
        setIsLoading(false);
      }
    };

    fetchProfile();
  }, []);

  const handleAvatarChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      setError('Please select an image file');
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setError('File size should be less than 5MB');
      return;
    }

    setAvatarFile(file);
    const reader = new FileReader();
    reader.onload = () => {
      setAvatarPreview(reader.result as string);
    };
    reader.readAsDataURL(file);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    
    if (name === 'bio' && value.length > 70) return;
    
    setFormData(prev => ({ ...prev, [name]: value }));
    
    if (name === 'bio') {
      setBioLength(value.length);
    }
  };

  const handleSave = async () => {
    try {
      setIsLoading(true);
      
      const profileResponse = await fetch('/api/users/profile', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(formData)
      });

      if (!profileResponse.ok) {
        throw new Error('Failed to update profile');
      }

      const updatedProfile = await profileResponse.json();
      setProfile(updatedProfile);

      if (avatarFile) {
        const formData = new FormData();
        formData.append('avatar', avatarFile);

        const avatarResponse = await fetch('/api/users/avatar', {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          },
          body: formData
        });

        if (!avatarResponse.ok) {
          throw new Error('Profile saved but avatar upload failed');
        }

        const { avatarUrl } = await avatarResponse.json();
        setProfile(prev => prev ? { ...prev, avatarUrl } : null);
      }

      setIsEditing(false);
      setAvatarFile(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save changes');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancel = () => {
    setIsEditing(false);
    setAvatarFile(null);
    setAvatarPreview(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  if (isLoading) return <div className="p-4 text-center">Loading...</div>;
  if (error) return <div className="p-4 text-red-500">{error}</div>;
  if (!profile) return <div className="p-4">Profile not found</div>;

  return (
    <div className="p-4 w-full mx-auto">
      <div className="flex justify-between items-center mb-8">
        <h2 className="text-2xl font-semibold">User Profile</h2>
        <div className="flex gap-2">
          {isEditing ? (
            <>
              <Button 
                useBaseClasses={false}
                onClick={handleSave}
                disabled={isLoading}
                className="p-2 h-fit w-fit rounded-full hover:bg-gray-700 transition-colors"
              >
                <SaveIcon className="w-4 h-4" />
              </Button>
              <Button 
                useBaseClasses={false}
                onClick={handleCancel}
                className="p-2 h-fit w-fit rounded-full hover:bg-gray-700 transition-colors"
              >
                <CancelIcon className="w-5 h-5" />
              </Button>
            </>
          ) : (
            <>
              <Button 
                useBaseClasses={false}
                onClick={() => setIsEditing(true)}
                className="p-2 h-fit w-fit rounded-full hover:bg-gray-700 transition-colors"
              >
                <EditIcon className="w-5 h-5" />
              </Button>
              <Button 
                useBaseClasses={false}
                onClick={onClose}
                className="p-2 h-fit w-fit rounded-full hover:bg-gray-700 transition-colors"
              >
                <LeftArrowIcon className="w-5 h-5" />
              </Button>
            </>
          )}
        </div>
      </div>

      {/* Avatar Section */}
      <div className="flex flex-col items-center mb-8">
        <div className="relative group mb-4">
          <Avatar 
            src={avatarPreview || profile.avatarUrl} 
            name={profile.username} 
            className="h-32 w-32 text-4xl"
          />
          {isEditing && (
            <div className="absolute inset-0 bg-black/50 rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
              <label className="cursor-pointer p-2 bg-gray-700 rounded-full">
                <input 
                  ref={fileInputRef}
                  type="file" 
                  className="hidden" 
                  accept="image/*"
                  onChange={handleAvatarChange}
                />
                <EditIcon className="w-5 h-5 text-white" />
              </label>
            </div>
          )}
        </div>
        <h3 className="text-xl font-medium text-gray-200">@{profile.username}</h3>
      </div>

      {/* Profile Info Section */}
      <div className="space-y-6 px-4">
        {isEditing ? (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <InputField
                label="First Name"
                name="firstName"
                value={formData.firstName || ''}
                onChange={handleInputChange}
                className="w-full"
              />
              <InputField
                label="Last Name"
                name="lastName"
                value={formData.lastName || ''}
                onChange={handleInputChange}
                className="w-full"
              />
            </div>
            <InputField
              label="Email"
              name="email"
              type="email"
              value={formData.email || ''}
              onChange={handleInputChange}
              className="w-full"
            />
            <InputField
              label="Phone Number"
              name="phoneNumber"
              value={formData.phoneNumber || ''}
              onChange={handleInputChange}
              className="w-full"
            />
            <div className="w-full">
              <TextArea
                label={`Bio (${bioLength}/70)`}
                name="bio"
                value={formData.bio || ''}
                onChange={handleInputChange}
                rows={3}
                className="w-full"
              />
              <p className="text-xs text-gray-400 mt-1">
                Maximum 70 characters allowed
              </p>
            </div>
          </>
        ) : (
          <>
            <div className="space-y-1">
              <h3 className="text-lg font-medium text-gray-300">Personal Information</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">
                <div>
                  <p className="text-sm text-gray-400">First Name</p>
                  <p className="truncate">{profile.firstName || '-'}</p>
                </div>
                <div>
                  <p className="text-sm text-gray-400">Last Name</p>
                  <p className="truncate">{profile.lastName || '-'}</p>
                </div>
              </div>
            </div>

            <div className="space-y-4">
              <div>
                <p className="text-sm text-gray-400">Email</p>
                <p className="truncate">{profile.email || '-'}</p>
              </div>
              <div>
                <p className="text-sm text-gray-400">Phone Number</p>
                <p className="truncate">{profile.phoneNumber || '-'}</p>
              </div>
              <div>
                <p className="text-sm text-gray-400">Bio</p>
                <p className="whitespace-pre-line break-words">
                  {profile.bio || 'No bio provided'}
                </p>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
};