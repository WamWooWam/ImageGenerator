import { useEffect, useReducer, useState } from "preact/hooks"

export default function Root() {
    const [text, setText] = useState("It looks like you're\nwriting a letter.\n\nWould you like some\nhelp with that?")
    const [character, setCharacter] = useState("clippit");
    const [font, setFont] = useState("comic");
    const [antialias, setAntialias] = useState(false);
    const [url, setUrl] = useState(null);

    useEffect(() => {
        setUrl(`/assistant/${character}/generate?text=${encodeURIComponent(text)}&font=${font}&antialias=${antialias}`);
    }, [text, character, font, antialias])

    const onDownload = async () => {
        const resp = await fetch(url, { mode: 'no-cors' });
        const blob = await resp.blob();

        const obj = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = obj;
        a.download = `${character}.png`
        a.click();
    }

    const onCopy = () => {
        navigator.clipboard.writeText(`https://${location.hostname}${url}`);
    }

    return (
        <>
            <div class="window main-window">
                <div class="title-bar">
                    <div class="title-bar-text">Microsoft® Office™ Assistant</div>
                </div>
                <div class="window-body">
                    <div class="main-content">
                        <menu role="tablist">
                            <li role="tab" aria-selected="true"><a href="#tabs">Assistant</a></li>
                        </menu>
                        <div class="window" role="tabpanel">
                            <div class="window-body">
                                <fieldset>
                                    <legend>Main Options</legend>
                                    <div class="field">
                                        <label for="text1">Text</label>
                                        <textarea id="text1"
                                            class="primary-text-area"
                                            value={text}
                                            maxLength={2048}
                                            onChange={(e) => setText((e.target as HTMLInputElement).value)}
                                            onInput={(e) => setText((e.target as HTMLInputElement).value)} />
                                    </div>

                                    <div class="field">
                                        <label for="character-select">Character</label>
                                        <select id="character-select"
                                            value={character}
                                            onChange={(e) => setCharacter((e.target as HTMLInputElement).value)}>
                                            <option value="clippit">Clippit</option>
                                            <option value="the_dot">The Dot</option>
                                            <option value="f1">F1</option>
                                            <option value="mother_earth">Mother Earth</option>
                                            <option value="office_logo">Office Logo</option>
                                            <option value="rocky">Rocky</option>
                                            <option value="links">Links</option>
                                            <option value="merlin">Merlin</option>
                                            <option value="the_genius">The Genius</option>
                                            <option value="rover">Rover</option>
                                            <option value="that_fucking_purple_monkey">Bonzi Buddy</option>
                                        </select>
                                    </div>
                                    <div class="field">
                                        <label for="font-select">Font</label>
                                        <select id="font-select"
                                            value={font}
                                            onChange={(e) => setFont((e.target as HTMLInputElement).value)}>
                                            <option value="comic">Comic Sans MS</option>
                                            <option value="times">Times New Roman</option>
                                            <option value="tahoma">Tahoma</option>
                                            <option value="sans_serif">Microsoft Sans Serif</option>
                                            <option value="courier">Courier New</option>
                                            <option value="gothic">MS Gothic</option>
                                            <option value="papyrus">Papyrus</option>
                                        </select>
                                    </div>
                                    <div class="field">
                                        <input type="checkbox" id="cleartype" checked={antialias} onChange={(e) => setAntialias((e.target as HTMLInputElement).checked)} />
                                        <label for="cleartype">Enable ClearType® Font Smoothing</label>
                                    </div>
                                </fieldset>
                            </div>
                        </div>
                    </div>
                    <div class="buttons">
                        <button onClick={onCopy}>Copy Link</button>
                        <button onClick={onDownload}>Download</button>
                    </div>
                </div>
            </div>

            <img class="assistant"
                src={url} />

            <p class="credit">Made by <a href="https://wamwoowam.co.uk">WamWooWam</a> - <a href="https://bsky.app/profile/wamwoowam.co.uk">@wamwoowam.co.uk</a></p>
        </>
    )
}