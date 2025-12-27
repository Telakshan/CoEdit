import React, { useRef, useEffect } from 'react';

interface EditorProps {
    initialContent: string;
    onContentChange: (html: string) => void;
    isLoading: boolean;
}

const Editor: React.FC<EditorProps> = ({ initialContent, onContentChange, isLoading }) => {
    const contentRef = useRef<HTMLDivElement>(null);
    useEffect(() => {
        if (contentRef.current && initialContent !== contentRef.current.innerHTML) {
            contentRef.current.innerHTML = initialContent;
        }
    }, [initialContent]);

    const handleInput = () => {
        if (contentRef.current) {
            onContentChange(contentRef.current.innerHTML);
        }
    };

    return (
        <div className="w-full max-w-[850px] mx-auto my-6 sm:my-8 px-4 sm:px-8 pb-32">
            <div
                className={`
           bg-white dark:bg-gray-800 min-h-[60vh] sm:min-h-[1100px] 
           p-6 sm:p-12 md:p-16 
           shadow-soft rounded-sm transition-all duration-500
           ${isLoading ? 'opacity-50 blur-[1px] pointer-events-none select-none' : 'opacity-100'}
        `}
            >
                <div
                    ref={contentRef}
                    contentEditable
                    onInput={handleInput}
                    suppressContentEditableWarning
                    className="editor-content outline-none max-w-none prose prose-lg prose-slate dark:prose-invert focus:outline-none"
                    spellCheck={false}
                />
            </div>
        </div>
    );
};

export default Editor;