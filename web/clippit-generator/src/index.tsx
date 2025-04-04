
if (process.env.NODE_ENV === "development") {
    require("preact/debug");
}

import "98.css"
import "./index.scss"

import Root from "./Root";
import { render } from "preact";

render(<Root />, document.getElementById('app'));