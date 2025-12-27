import React, { useState } from 'react';
import {
    Bold, Italic, Underline, Strikethrough,
    AlignLeft, AlignCenter, AlignRight, AlignJustify,
    List, Heading2, Highlighter, Plus, Minus, ChevronDown,
    Moon, Sun
} from './Icons';

interface ToolbarProps {
    onFormat: (command: string, value?: string) => void;
    isDarkMode: boolean;
    toggleDarkMode: () => void;
}

const Toolbar: React.FC<ToolbarProps> = ({ onFormat, isDarkMode, toggleDarkMode }) => {
    const [showColorPicker, setShowColorPicker] = useState(false);

    const colors = [
        { name: 'Green', value: '#dcfce7', border: '#22c55e' },
        { name: 'Blue', value: '#dbeafe', border: '#3b82f6' },
        { name: 'Red', value: '#fee2e2', border: '#ef4444' },
        { name: 'Purple', value: '#f3e8ff', border: '#a855f7' },
        { name: 'Yellow', value: '#fef9c3', border: '#eab308' },
        { name: 'None', value: 'transparent', border: '#e5e7eb' },
    ];

    const handleMouseDown = (e: React.MouseEvent) => {
        e.preventDefault(); // Prevent loss of focus from editor
    };

    return (
        <div className="flex items-center justify-center p-2 bg-white/80 dark:bg-gray-900/80 backdrop-blur-md sticky top-0 z-50 border-b border-gray-100 dark:border-gray-800 transition-all duration-300">
            <div className="flex items-center space-x-1 md:space-x-2 bg-white dark:bg-gray-800 px-4 py-1.5 rounded-full shadow-sm border border-gray-200/60 dark:border-gray-700 overflow-x-auto max-w-full scrollbar-hide">

                {/* Dark Mode Toggle */}
                <button
                    onClick={toggleDarkMode}
                    onMouseDown={handleMouseDown}
                    className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-500 dark:text-gray-400"
                    title={isDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode"}
                >
                    {isDarkMode ? <Sun size={18} /> : <Moon size={18} />}
                </button>

                <div className="w-px h-4 bg-gray-200 dark:bg-gray-700 mx-1"></div>

                {/* Text Styles */}
                <button onMouseDown={handleMouseDown} onClick={() => onFormat('formatBlock', 'H2')} className="flex items-center space-x-1 px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-purple-600 dark:text-purple-400 bg-purple-50 dark:bg-purple-900/20 font-medium text-sm whitespace-nowrap">
                    <span>H2</span>
                    <ChevronDown size={12} />
                </button>

                <button onMouseDown={handleMouseDown} onClick={() => onFormat('insertUnorderedList')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-600 dark:text-gray-300">
                    <List size={18} />
                </button>

                <div className="w-px h-4 bg-gray-200 dark:bg-gray-700 mx-1"></div>

                <button onMouseDown={handleMouseDown} onClick={() => onFormat('bold')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-700 dark:text-gray-200 font-bold"><Bold size={18} /></button>
                <button onMouseDown={handleMouseDown} onClick={() => onFormat('italic')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-700 dark:text-gray-200 italic"><Italic size={18} /></button>
                <button onMouseDown={handleMouseDown} onClick={() => onFormat('strikeThrough')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-700 dark:text-gray-200 line-through"><Strikethrough size={18} /></button>
                <button onMouseDown={handleMouseDown} onClick={() => onFormat('underline')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-700 dark:text-gray-200 underline"><Underline size={18} /></button>

                {/* Color Picker */}
                <div className="relative">
                    <button
                        onMouseDown={handleMouseDown}
                        onClick={() => setShowColorPicker(!showColorPicker)}
                        className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-purple-600 dark:text-purple-400 flex items-center"
                    >
                        <Highlighter size={18} />
                    </button>

                    {showColorPicker && (
                        <div className="absolute top-full mt-2 left-1/2 -translate-x-1/2 bg-white dark:bg-gray-800 p-2 rounded-xl shadow-float border border-gray-100 dark:border-gray-700 flex space-x-2 z-50 animate-in fade-in zoom-in-95 duration-200">
                            {colors.map((c) => (
                                <button
                                    key={c.name}
                                    onMouseDown={handleMouseDown}
                                    onClick={() => {
                                        onFormat('hiliteColor', c.value);
                                        setShowColorPicker(false);
                                    }}
                                    className="w-6 h-6 rounded-full border hover:scale-110 transition-transform"
                                    style={{ backgroundColor: c.value, borderColor: c.border }}
                                    title={c.name}
                                />
                            ))}
                            <button
                                onMouseDown={handleMouseDown}
                                onClick={() => setShowColorPicker(false)}
                                className="w-6 h-6 rounded-full border border-gray-200 dark:border-gray-600 flex items-center justify-center text-gray-400 dark:text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
                            >
                                <div className="w-3 h-px bg-current rotate-45 absolute" />
                            </button>
                        </div>
                    )}
                </div>

                <div className="w-px h-4 bg-gray-200 dark:bg-gray-700 mx-1"></div>

                {/* Alignment */}
                <button onMouseDown={handleMouseDown} onClick={() => onFormat('justifyLeft')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-600 dark:text-gray-300"><AlignLeft size={18} /></button>
                <button onMouseDown={handleMouseDown} onClick={() => onFormat('justifyCenter')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-600 dark:text-gray-300"><AlignCenter size={18} /></button>
                <button onMouseDown={handleMouseDown} onClick={() => onFormat('justifyRight')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-600 dark:text-gray-300"><AlignRight size={18} /></button>
                <button onMouseDown={handleMouseDown} onClick={() => onFormat('justifyFull')} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-600 dark:text-gray-300"><AlignJustify size={18} /></button>

                <div className="w-px h-4 bg-gray-200 dark:bg-gray-700 mx-1"></div>

                <button onMouseDown={handleMouseDown} className="flex items-center space-x-1 px-3 py-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-500 dark:text-gray-400 text-sm font-medium whitespace-nowrap">
                    <Plus size={14} />
                    <span>Add</span>
                </button>

            </div>
        </div>
    );
};

export default Toolbar;