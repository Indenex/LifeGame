# wiki-page

Builds the single-page web version of `wiki/` that the PM publishes as Justin's private artifact (https://claude.ai/artifact/4sKf1fGqBZEQeErZCkibaB).

    python3 tools/wiki-page/build_wiki.py wiki out.html tools/wiki-page/template.html

Then publish `out.html` to that artifact URL with the Artifact tool. Page order is the `order` list in `build_wiki.py`; add new pages there.
