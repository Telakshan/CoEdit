import React, { useState, useCallback, useEffect } from 'react';
import Toolbar from './components/Toolbar';
import Editor from './components/Editor';

const INITIAL_CONTENT = `
<h1>Cover Page</h1>
<p><strong>Project Title:</strong> <em>Enhancing Neural Plasticity Through Multi-Modal Interventions in Aging Adults: A Randomized Controlled Trial</em></p>

<p><strong>Principal Investigator (PI):</strong> [Your Name, PhD]<br>
<strong>Institution:</strong> [Your University or Research Center]<br>
<strong>Funding Mechanism:</strong> NIH R01 Research Project Grant<br>
<strong>Proposed Project Period:</strong> 24 months<br>
<strong>Total Direct Costs Requested:</strong> <span style="background-color: #fef9c3; padding: 0 4px;">[To be determined]</span></p>

<p><strong>Keywords:</strong> Cognitive aging, neural plasticity, fMRI, EEG, cognitive training, physical exercise, nutritional supplementation</p>

<h3>Collaborating Institutions:</h3>
<ul>
  <li>[Institution A] – Neuroimaging Core</li>
  <li>[Institution B] – Exercise Physiology Unit</li>
  <li>[Institution C] – Nutritional Sciences Department</li>
</ul>

<h3>Abstract:</h3>
<p>This study aims to investigate the synergistic effects of cognitive training, aerobic exercise, and nutritional supplementation on neural plasticity in aging adults. We hypothesize that a multi-modal approach will yield superior outcomes compared to single-modality interventions.</p>
`;

const App: React.FC = () => {
  const [content, setContent] = useState(INITIAL_CONTENT);
  const [isLoading, setIsLoading] = useState(false);
  const [isDarkMode, setIsDarkMode] = useState(false);

  useEffect(() => {
    const savedTheme = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
      setIsDarkMode(true);
      document.documentElement.classList.add('dark');
    } else {
      setIsDarkMode(false);
      document.documentElement.classList.remove('dark');
    }
  }, []);

  const toggleDarkMode = () => {
    const newMode = !isDarkMode;
    setIsDarkMode(newMode);
    if (newMode) {
      document.documentElement.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      document.documentElement.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  };

  const handleContentChange = useCallback((newHtml: string) => {
    setContent(newHtml);
  }, []);


  const handleFormat = (command: string, value?: string) => {
    document.execCommand(command, false, value);
    const editor = document.querySelector('.editor-content');
    if (editor) {
      setContent(editor.innerHTML);
    }
  };

  return (
    <div className="flex flex-col h-screen overflow-hidden bg-[#f9f9fb] dark:bg-gray-950 transition-colors duration-300">
      <Toolbar onFormat={handleFormat} isDarkMode={isDarkMode} toggleDarkMode={toggleDarkMode} />

      <main className="flex-1 overflow-y-auto relative">
        <Editor
          initialContent={content}
          onContentChange={handleContentChange}
          isLoading={isLoading}
        />
      </main>

    </div>
  );
};

export default App;