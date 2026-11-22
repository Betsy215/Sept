# step the day until a customer is thinking (three dots), then return; $1 = the most steps
for i in $(seq 1 ${1:-40}); do r=$($M exec Docs/tools/recipes/r32/dots_state.cs | grep -o '"result":"[^"]*"'); case "$r" in *"dots in"*) exit 0;; esac; $M exec Docs/tools/recipes/r26/step_dish.cs >/dev/null; sleep 0.5; done; exit 1
