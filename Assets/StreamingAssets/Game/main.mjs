import { buildTournament, createRng, rankTable, simulateMatch } from "../src/game/index.mjs";

const squads = window.WORLD_CUP_SQUADS || [];
const state = {
  rng: createRng(8080),
  ratings: { def: 84, mid: 86, att: 88, ovr: 86 },
  tournament: null,
  current: 0,
  history: []
};

const formationName = document.getElementById("formationName");
const ratingsEl = document.getElementById("ratings");
const scheduleEl = document.getElementById("schedule");
const tableEl = document.getElementById("table");
const newRunBtn = document.getElementById("newRunBtn");
const simulateBtn = document.getElementById("simulateBtn");

newRunBtn.addEventListener("click", startRun);
simulateBtn.addEventListener("click", simulateNext);

startRun();

function startRun() {
  state.rng = createRng(Date.now());
  state.tournament = buildTournament({ squads, ratings: state.ratings, rng: state.rng });
  state.current = 0;
  state.history = [];
  formationName.textContent = "4-3-3";
  render();
}

function simulateNext() {
  const item = state.tournament.schedule[state.current];
  if (!item) return;
  const result = simulateMatch({ ratings: state.ratings, opponent: item.opponent, roundIndex: state.current, rng: state.rng });
  state.history.push({ ...item, ...result });
  if (state.current < 3) {
    const group = state.tournament.groups[0];
    applyLocalGroup(group, "8-0 XI", item.opponent.name, result.home, result.away);
  }
  state.current += 1;
  render();
}

function applyLocalGroup(group, homeName, awayName, homeGoals, awayGoals) {
  const home = group.table.find(team => team.name === homeName);
  const away = group.table.find(team => team.name === awayName);
  if (!home || !away) return;
  applyStanding(home, homeGoals, awayGoals);
  applyStanding(away, awayGoals, homeGoals);
}

function applyStanding(team, gf, ga) {
  team.played += 1;
  team.gf += gf;
  team.ga += ga;
  team.gd = team.gf - team.ga;
  if (gf > ga) {
    team.wins += 1;
    team.pts += 3;
  } else if (gf === ga) {
    team.draws += 1;
    team.pts += 1;
  } else {
    team.losses += 1;
  }
}

function render() {
  ratingsEl.innerHTML = Object.entries(state.ratings).map(([key, value]) => `
    <div class="rating"><small>${key.toUpperCase()}</small><b>${value}</b></div>
  `).join("");

  scheduleEl.innerHTML = state.tournament.schedule.map((match, index) => {
    const played = state.history[index];
    const scoreClass = played ? played.home > played.away ? "win" : played.home === played.away ? "draw" : "loss" : "";
    return `
      <div class="match">
        <span><small>${match.phase}</small><br>vs ${match.opponent.name}</span>
        <b class="score ${scoreClass}">${played ? `${played.home}-${played.away}` : "-"}</b>
      </div>
    `;
  }).join("");

  tableEl.innerHTML = rankTable(state.tournament.groups[0]).map((team, index) => `
    <div class="row">
      <span><small>${index + 1}</small> ${team.name}</span>
      <b>${team.pts} pts</b>
    </div>
  `).join("");
}
