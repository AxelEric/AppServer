import React from "react";
import PropTypes from "prop-types";
import IconButton from "../icon-button";
import Tooltip from "../tooltip";
import { handleAnyClick } from "../../utils/event";
import uniqueId from "lodash/uniqueId";

class HelpButton extends React.Component {
  constructor(props) {
    super(props);

    this.state = { isOpen: false };
    this.ref = React.createRef();
    this.refTooltip = React.createRef();
    this.id = `tooltip_${uniqueId()}`;
  }

  afterShow = () => {
    this.refTooltip.current.updatePosition();
    //console.log(`afterShow ${this.props.tooltipId} isOpen=${this.state.isOpen}`, this.ref, e);
    this.setState({ isOpen: true }, () => {
      handleAnyClick(true, this.handleClick);
    });
  };

  afterHide = () => {
    //console.log(`afterHide ${this.props.tooltipId} isOpen=${this.state.isOpen}`, this.ref, e);
    if (this.state.isOpen) {
      this.setState({ isOpen: false }, () => {
        handleAnyClick(false, this.handleClick);
      });
    }
  };

  handleClick = e => {
    //console.log(`handleClick ${this.props.tooltipId} isOpen=${this.state.isOpen}`, this.ref, e);

    if (!this.ref.current.contains(e.target)) {
      //console.log(`hideTooltip() tooltipId=${this.props.tooltipId}`, this.refTooltip.current);
      this.refTooltip.current.hideTooltip();
    }
  };

  componentWillUnmount() {
    handleAnyClick(false, this.handleClick);
  }

  render() {
    const { tooltipContent, place, offsetRight, offsetLeft, iconName, color, getContent, className, dataTip } = this.props;

    return (
      <div ref={this.ref}>
        <IconButton
          id={this.id}
          className={className}
          isClickable={true}
          iconName={iconName}
          size={13}
          color={color}
          data-for={this.id}
          dataTip={dataTip}
        />
        {getContent ? 
        <Tooltip
          id={this.id}
          reference={this.refTooltip}
          effect="solid"
          place={place}
          offsetRight={offsetRight}
          offsetLeft={offsetLeft}
          afterShow={this.afterShow}
          afterHide={this.afterHide}
          getContent={getContent}
        />
        :
        <Tooltip
          id={this.id}
          reference={this.refTooltip}
          effect="solid"
          place={place}
          offsetRight={offsetRight}
          offsetLeft={offsetLeft}
          afterShow={this.afterShow}
          afterHide={this.afterHide}
          getContent={getContent}
        >
          {tooltipContent}
        </Tooltip>
        }
      </div>
    );
  }
}

HelpButton.propTypes = {
  children: PropTypes.oneOfType([
    PropTypes.arrayOf(PropTypes.node),
    PropTypes.node
  ]),
  tooltipContent: PropTypes.oneOfType([PropTypes.string, PropTypes.object]),
  offsetRight: PropTypes.number,
  tooltipMaxWidth: PropTypes.number,
  tooltipId: PropTypes.string,
  place: PropTypes.string,
  offsetLeft: PropTypes.number,
  iconName: PropTypes.string,
  color: PropTypes.string,
  className: PropTypes.string,
  dataTip: PropTypes.string,
  getContent: PropTypes.func
};

HelpButton.defaultProps = {
  place: "top",
  offsetRight: 120,
  offsetLeft: 0,
  iconName: "QuestionIcon"
}

export default HelpButton;
