'use client';
import { useState, useRef, useEffect } from 'react';
import { DropdownMenu } from './DropdownMenu';
import { DropdownMenuItem } from './DropdownMenuItem';
import { BurgerMenuIcon } from '@/components/icons/BurgerMenuIcon';
import { Button } from '@/components/ui/Button/Button';

interface BurgerMenuProps {
  className?: string;
  menuItems?: {
    label: string;
    onClick: () => void;
    danger?: boolean;
  }[];
}

export const BurgerMenu = ({ className = '', menuItems = [] }: BurgerMenuProps) => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  const toggleMenu = () => {
    setIsMenuOpen(prev => !prev);
  };

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        buttonRef.current?.contains(event.target as Node) ||
        menuRef.current?.contains(event.target as Node)
      ) {
        return;
      }
      setIsMenuOpen(false);
    };

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setIsMenuOpen(false);
    };

    if (isMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      document.addEventListener('keydown', handleKeyDown);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isMenuOpen]);

  return (
    <div className={`relative ${className}`}>
      <Button
        ref={buttonRef}
        className="flex items-center justify-center"
        onClick={toggleMenu}
        aria-label="Menu"
        aria-expanded={isMenuOpen}
        useBaseClasses={false}
      >
        <BurgerMenuIcon />
      </Button>

      <DropdownMenu isOpen={isMenuOpen} ref={menuRef}>
        <div className="p-2 space-y-1">
          {menuItems.map((item, index) => (
            <DropdownMenuItem 
              key={index}
              onClick={(e) => {
                e.stopPropagation();
                item.onClick();
                setIsMenuOpen(false);
              }}
              danger={item.danger}
            >
              {item.label}
            </DropdownMenuItem>
          ))}
        </div>
      </DropdownMenu>
    </div>
  );
};