requirejs(["./Page", "./Header"], (Page, Header) => {
  const App = () => (
    <div>foo</div>
  );
  
  ReactDOM.render(<App />, document.getElementById("root"));
});
